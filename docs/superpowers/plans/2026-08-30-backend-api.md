# Бек KanbanBoard: санитария и ordering — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox ("- [ ]") syntax for tracking.

**Goal:** довести бек KanbanBoard до состояния, в котором на нём можно писать фронт из спеки `docs/superpowers/specs/2026-08-30-kanban-frontend-design.md`: починить найденные баги и дыры (фаза 1 «санитария»), добавить порядок колонок/карточек, оба `/position`-эндпоинта, CRUD колонок, GET одной задачи и историю (фаза 2). Это фазы 1–2 из §10 спеки; фронт (фазы 3–5) — отдельный план.

**Architecture:** существующая многослойка ASP.NET Core MVC: `Controllers/*` (тонкие, только достают `userId` из клеймов, зовут сервис и мапят `null → 404`) → `Services/*` (вся логика и проверки прав, работают с `KanbanBoardDbContext` напрямую) → `Models/{Requests,Responses}` (плоские DTO). Никаких новых слоёв, репозиториев и мапперов не вводим. Права: «не участник / нельзя» → `404`; `403` не используем (кроме автоматического от cookie-хендлера). Тексты ошибок — плоские русские строки.

**Tech Stack:** .NET 9, ASP.NET Core MVC + `[ApiController]`, EF Core 9.0.17 + Npgsql, PostgreSQL (`KanbanBoardDb` на `localhost:5432`), cookie-аутентификация, Swashbuckle 10.2.3, BCrypt.Net-Next. Тестовой инфраструктуры в проекте нет и мы её не заводим (решение Павла, §9 спеки) — проверка = `dotnet build` + curl по живому приложению на `http://localhost:5110`.

---

## Структура файлов

### Создаются

| Файл | Ответственность |
|---|---|
| `Models/Requests/StatusRequest.cs` | тело создания/переименования колонки: `Name` |
| `Models/Requests/StatusPositionRequest.cs` | тело `PATCH /statuses/{id}/position`: `Position` |
| `Models/Requests/TaskPositionRequest.cs` | тело `PATCH /tasks/{id}/position`: `StatusId`, `Position` |
| `Models/Responses/StatusPositionResponse.cs` | элемент ответа на перестановку колонки: `{ Id, Order }` |
| `Models/Responses/TaskPositionResponse.cs` | элемент ответа на перенос задачи: `{ Id, StatusId, Order }` |
| `Migrations/<timestamp>_AddOrdering.cs` (+ `.Designer.cs`) | миграция: `Status.Order`, `Task.Order` + бекфилл (генерируется `dotnet ef`) |

### Изменяются

| Файл | Что меняется |
|---|---|
| `Program.cs` | `UseStaticFiles`, заголовок Swagger, `OnRedirectToAccessDenied`, `SuppressMapClientErrors` |
| `Controllers/AuthController.cs` | `[ApiController]`, `401` на неверный логин/пароль, `[Authorize]` на `/me` и `/logout`, `401` на протухшую куку |
| `Controllers/BoardController.cs` | `[ApiController]` |
| `Controllers/UserController.cs` | `[ApiController]` |
| `Controllers/AttachmentController.cs` | `[ApiController]` |
| `Controllers/CommentController.cs` | `[ApiController]` |
| `Controllers/StatusController.cs` | `[ApiController]`, CRUD колонок, `PATCH /position` |
| `Controllers/TaskController.cs` | `[ApiController]`, `GET` одной задачи, `GET` истории, `PATCH /position` |
| `Services/AuthService.cs` | `GetUserInfo` возвращает `null` вместо исключения |
| `Services/BoardService.cs` | `Author.Login` в списке досок, создании и обновлении |
| `Services/StatusService.cs` | сортировка `(Order, StatusId)`, `Order` в ответе и дефолтных колонках, CRUD, перестановка |
| `Services/TaskService.cs` | общая проекция `TaskResponse`, nullable-исполнитель, `WorkerId`, `Order`, история, `GET` одной задачи, перенос |
| `Services/CommentService.cs` | скоупинг задачи по `boardId`, вложения и `TaskId` в `CommentResponse` |
| `Services/AttachmentService.cs` | перестаёт отдавать `FilePath` |
| `Models/Status.cs` | поле `Order` |
| `Models/Task.cs` | поле `Order`, навигация `Assignee` становится nullable |
| `Models/Requests/BoardRequest.cs` | `[Required]`/`[MaxLength]`, `Description` становится nullable |
| `Models/Requests/BoardUserRequest.cs` | `[Required]`/`[MaxLength]` |
| `Models/Requests/CommentRequest.cs` | `[Required]`/`[MaxLength]` |
| `Models/Requests/LoginRequest.cs` | `[Required]`/`[MaxLength]` |
| `Models/Requests/RegisterRequest.cs` | `[Required]`/`[MaxLength]`/`[MinLength(6)]` |
| `Models/Requests/StatusHistoryRequest.cs` | `[Range]` |
| `Models/Requests/TaskRequest.cs` | `[Required]`/`[MaxLength]`, `WorkerId` становится `int?` |
| `Models/Responses/StatusResponse.cs` | поле `Order` |
| `Models/Responses/TaskResponse.cs` | `BoardId`, `Order`, `Worker` nullable, `CommentsCount`, `AttachmentsCount` |
| `Models/Responses/CommentResponse.cs` | `TaskId`, `Text` nullable, `Attachments` |
| `Models/Responses/StatusHistoryResponse.cs` | `StatusChangeId`, `Author` |
| `Models/Responses/AttachmentResponse.cs` | убирается `FilePath` |

---

## Команды окружения

Эти команды используются в шагах проверки дословно.

**Сборка:**

```bash
cd . && dotnet build 2>&1 | tail -5
```

Ожидаемо: строка вида `Build succeeded.` и `0 Error(s)`.

**Перезапуск приложения (после каждого изменения кода):**

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && curl -s -o /dev/null -w "ping=%{http_code}\n" http://localhost:5110/
```

Ожидаемо: `0 Error(s)` и `ping=200`.

**Логин тестовым пользователем (кука кладётся в `/tmp/kb.cookies`):**

```bash
curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "register=%{http_code}\n" ; curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n"
```

Ожидаемо: `register=200` (или `register=409`, если пользователь уже создан прошлым прогоном) и `login=200`.

**Требование:** PostgreSQL должен быть запущен, база `KanbanBoardDb` доступна по строке из `appsettings.json`. Если приложение не поднимается — смотреть `/tmp/kb-run.log`.

---

# ФАЗА 1 — САНИТАРИЯ

## Task 1: Program.cs — статика, Swagger, OnRedirectToAccessDenied

**Files:**
- Modify: `./Program.cs` (весь файл; строка 48 содержит битую в кодировке строку заголовка Swagger, поэтому файл переписывается целиком)

- [ ] Прочитать `Program.cs` целиком (Read), затем перезаписать (Write) следующим содержимым:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<KanbanBoardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BoardService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<StatusService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KanbanBoard API v1");
        c.InjectJavascript("/swagger-ui/custom.js");
    });
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
```

- [ ] Проверка сборки и запуска:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && curl -s -o /dev/null -w "ping=%{http_code}\n" http://localhost:5110/
```

Ожидаемо: `0 Error(s)`, `ping=200`.

- [ ] Проверка заголовка Swagger:

```bash
curl -s http://localhost:5110/swagger/index.html -o /dev/null -w "swagger=%{http_code}\n" ; curl -s http://localhost:5110/swagger/v1/swagger.json -o /dev/null -w "swaggerjson=%{http_code}\n"
```

Ожидаемо: `swagger=200`, `swaggerjson=200`. Дополнительно открыть `http://localhost:5110/swagger` в браузере и убедиться, что в выпадающем списке определений написано `KanbanBoard API v1`, а не крякозябры.

- [ ] Коммит:

```bash
cd . && git add Program.cs && git commit -m "Бек: раздача статики, заголовок Swagger и 403 для /api при отказе в доступе"
```

---

## Task 2: Валидация request-DTO

**Files:**
- Modify: `./Models/Requests/BoardRequest.cs`
- Modify: `./Models/Requests/BoardUserRequest.cs`
- Modify: `./Models/Requests/CommentRequest.cs`
- Modify: `./Models/Requests/LoginRequest.cs`
- Modify: `./Models/Requests/RegisterRequest.cs`
- Modify: `./Models/Requests/StatusHistoryRequest.cs`
- Modify: `./Models/Requests/TaskRequest.cs`

- [ ] Перезаписать `Models/Requests/BoardRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class BoardRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(3000)]
        public string? Description { get; set; }
    }
}
```

- [ ] Перезаписать `Models/Requests/BoardUserRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class BoardUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string Login { get; set; } = null!;
    }
}
```

- [ ] Перезаписать `Models/Requests/CommentRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class CommentRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Text { get; set; } = null!;
    }
}
```

- [ ] Перезаписать `Models/Requests/LoginRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests;

public class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
```

- [ ] Перезаписать `Models/Requests/RegisterRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;
}
```

- [ ] Перезаписать `Models/Requests/StatusHistoryRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class StatusHistoryRequest
    {
        [Range(1, int.MaxValue)]
        public int NewStatusId { get; set; }
    }
}
```

- [ ] Перезаписать `Models/Requests/TaskRequest.cs` (тип `WorkerId` пока не трогаем — он меняется в Task 12):

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class TaskRequest
    {
        [Required]
        [MaxLength(50)]
        public string TaskName { get; set; } = null!;

        [MaxLength(3000)]
        public string? TaskDescription { get; set; }

        public DateTime Deadline { get; set; }

        public int WorkerId { get; set; }
    }
}
```

- [ ] Проверка сборки:

```bash
cd . && dotnet build 2>&1 | tail -5
```

Ожидаемо: `Build succeeded.`, `0 Error(s)`. (Атрибуты пока ничего не валидируют — `[ApiController]` включается в Task 3.)

- [ ] Коммит:

```bash
cd . && git add Models/Requests && git commit -m "Бек: атрибуты валидации на request-DTO"
```

---

## Task 3: `[ApiController]` на семи API-контроллерах

**Files:**
- Modify: `./Controllers/AuthController.cs` (строка 11)
- Modify: `./Controllers/BoardController.cs` (строка 9–10)
- Modify: `./Controllers/StatusController.cs` (строка 8–9)
- Modify: `./Controllers/TaskController.cs` (строка 9–10)
- Modify: `./Controllers/CommentController.cs` (строка 9–10)
- Modify: `./Controllers/AttachmentController.cs` (строка 8–9)
- Modify: `./Controllers/UserController.cs` (строка 8–9)
- Modify: `./Program.cs`

`HomeController` НЕ трогаем — это MVC-контроллер без атрибутивного роутинга.

- [ ] В `Controllers/AuthController.cs` заменить:

```csharp
    public class AuthController : Controller
```

на:

```csharp
    [ApiController]
    public class AuthController : Controller
```

- [ ] В `Controllers/BoardController.cs` заменить:

```csharp
    [Authorize]
    public class BoardController(BoardService boardService) : Controller
```

на:

```csharp
    [ApiController]
    [Authorize]
    public class BoardController(BoardService boardService) : Controller
```

- [ ] В `Controllers/StatusController.cs` заменить:

```csharp
    [Authorize]
    public class StatusController(StatusService statusService) : Controller
```

на:

```csharp
    [ApiController]
    [Authorize]
    public class StatusController(StatusService statusService) : Controller
```

- [ ] В `Controllers/TaskController.cs` заменить:

```csharp
    [Authorize]
    public class TaskController(TaskService taskService) : Controller
```

на:

```csharp
    [ApiController]
    [Authorize]
    public class TaskController(TaskService taskService) : Controller
```

- [ ] В `Controllers/CommentController.cs` заменить:

```csharp
    [Authorize]
    public class CommentController(CommentService commentService) : Controller
```

на:

```csharp
    [ApiController]
    [Authorize]
    public class CommentController(CommentService commentService) : Controller
```

- [ ] В `Controllers/AttachmentController.cs` заменить:

```csharp
    [Authorize]
    public class AttachmentController(AttachmentService attachmentService) : Controller
```

на:

```csharp
    [ApiController]
    [Authorize]
    public class AttachmentController(AttachmentService attachmentService) : Controller
```

- [ ] В `Controllers/UserController.cs` заменить:

```csharp
    [Authorize]
    public class UserController(UserService userService) : Controller
```

на:

```csharp
    [ApiController]
    [Authorize]
    public class UserController(UserService userService) : Controller
```

- [ ] В `Program.cs` добавить `using Microsoft.AspNetCore.Mvc;`. Заменить:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
```

на:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
```

- [ ] В `Program.cs` отключить автоподмену пустых тел ошибок на `ProblemDetails`, чтобы `NotFound()` остался с пустым телом, как сейчас, а `400` от валидации остался `ValidationProblemDetails` (§3 спеки: «валидационный 400 — JSON, всё остальное — плоские строки»). Заменить:

```csharp
// Add services to the container.
builder.Services.AddControllersWithViews();
```

на:

```csharp
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
});
```

- [ ] Проверка: перезапустить приложение и проверить, что валидация отдаёт 400 `ValidationProblemDetails`, а не 500:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && curl -s -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"","password":"123"}' -w "\nHTTP=%{http_code}\n"
```

Ожидаемо: `HTTP=400`, в теле JSON с `"errors"`, внутри — ключи `Login` (обязательное поле) и `Password` (короче 6 символов).

- [ ] Проверка, что рабочие сценарии не сломались:

```bash
curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "register=%{http_code}\n" ; curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; curl -s -b /tmp/kb.cookies http://localhost:5110/api/boards -w "\nboards=%{http_code}\n"
```

Ожидаемо: `register=200` или `register=409`, `login=200`, `boards=200` и JSON-массив в теле.

- [ ] Коммит:

```bash
cd . && git add Controllers Program.cs && git commit -m "Бек: [ApiController] на API-контроллерах, кривой ввод даёт 400 вместо 500"
```

---

## Task 4: Auth — 401 вместо 409, `[Authorize]` на `/me` и `/logout`

**Files:**
- Modify: `./Controllers/AuthController.cs`
- Modify: `./Services/AuthService.cs` (метод `GetUserInfo`, строки 52–63)

- [ ] В `Services/AuthService.cs` заменить метод `GetUserInfo` целиком:

```csharp
        public async Task<GetUserInfoRespones> GetUserInfo(string login, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login, ct)
                       ?? throw new InvalidOperationException("Неверный логин!");

            return new GetUserInfoRespones()
            {
                UserId = user.UserId,
                Login = user.Login,
                DateOfRegistration = user.DateOfRegistration,
            };
        }
```

на:

```csharp
        public async Task<GetUserInfoRespones?> GetUserInfo(string login, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login, ct);

            if (user is null)
            {
                return null;
            }

            return new GetUserInfoRespones()
            {
                UserId = user.UserId,
                Login = user.Login,
                DateOfRegistration = user.DateOfRegistration,
            };
        }
```

- [ ] В `Controllers/AuthController.cs` заменить экшен `Login` целиком:

```csharp
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("api/auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model, CancellationToken ct)
        {
            var user = await _authService.Login(model, ct);

            if (user == null)
            {
                return Conflict("Такой логин отсутствует");
            }

            await SignInUser(user);

            return Ok();
        }
```

на:

```csharp
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model, CancellationToken ct)
        {
            var user = await _authService.Login(model, ct);

            if (user == null)
            {
                return Unauthorized("Неверный логин или пароль");
            }

            await SignInUser(user);

            return Ok();
        }
```

- [ ] В `Controllers/AuthController.cs` заменить экшены `UserInfo` и `Logout` целиком:

```csharp
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("api/auth/me")]
        public async Task<IActionResult> UserInfo(CancellationToken ct)
        {
            var login = User.FindFirstValue(ClaimTypes.Name);

            if (login == null)
            {
                return BadRequest("Не поняли кто!");
            }

            var result = await _authService.GetUserInfo(login, ct);

            return Ok(result);
        }

        [HttpPost]
        [Route("api/auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
```

на:

```csharp
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/auth/me")]
        public async Task<IActionResult> UserInfo(CancellationToken ct)
        {
            var login = User.FindFirstValue(ClaimTypes.Name);

            if (login == null)
            {
                return Unauthorized("Не авторизован");
            }

            var result = await _authService.GetUserInfo(login, ct);

            if (result == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Unauthorized("Не авторизован");
            }

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("api/auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
```

- [ ] Проверка: перезапустить приложение и прогнать сценарий:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"wrongpass"}' -w "\nbadlogin=%{http_code}\n" ; curl -s http://localhost:5110/api/auth/me -o /dev/null -w "anonme=%{http_code}\n" ; curl -s -X POST http://localhost:5110/api/auth/logout -o /dev/null -w "anonlogout=%{http_code}\n"
```

Ожидаемо: `badlogin=401` с телом `Неверный логин или пароль`, `anonme=401`, `anonlogout=401`.

- [ ] Проверка счастливого пути:

```bash
curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "register=%{http_code}\n" ; curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; curl -s -b /tmp/kb.cookies http://localhost:5110/api/auth/me -w "\nme=%{http_code}\n"
```

Ожидаемо: `login=200`, `me=200`, тело вида `{"userId":N,"login":"kbtest","dateOfRegistration":"..."}`.

- [ ] Коммит:

```bash
cd . && git add Controllers/AuthController.cs Services/AuthService.cs && git commit -m "Бек: 401 на неверный логин, авторизация на /me и /logout, 401 на протухшую куку"
```

---

## Task 5: Редактировать задачу может любой активный участник доски

**Files:**
- Modify: `./Services/TaskService.cs` (метод `UpdateTaskAsync`, строки 120–121)

- [ ] В `Services/TaskService.cs` в методе `UpdateTaskAsync` удалить проверку авторства. Заменить:

```csharp
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (task == null)
                return null;
            if (task.AuthorId != currentBoardUser.BoardUserId)
                return null;
```

на:

```csharp
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (task == null)
                return null;
```

- [ ] Проверка: перезапустить приложение и убедиться, что PUT задачи от участника-не-автора возвращает 200. Скрипт создаёт вторую учётку, доску, задачу, добавляет второго участника и правит задачу от его имени:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies /tmp/kb2.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null ; curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null ; curl -s -c /tmp/kb2.cookies -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"kbtest2","password":"secret123"}' -o /dev/null ; curl -s -c /tmp/kb2.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest2","password":"secret123"}' -o /dev/null ; BOARD=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Права","description":"проверка"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; ME=$(curl -s -b /tmp/kb.cookies http://localhost:5110/api/auth/me | python3 -c "import sys,json;print(json.load(sys.stdin)['userId'])") ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$BOARD/users" -H "Content-Type: application/json" -d '{"login":"kbtest2"}' -o /dev/null -w "adduser=%{http_code}\n" ; TASK=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$BOARD/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"Задача\",\"taskDescription\":\"текст\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$ME}" | python3 -c "import sys,json;print(json.load(sys.stdin)['taskId'])") ; echo "board=$BOARD task=$TASK" ; curl -s -b /tmp/kb2.cookies -X PUT "http://localhost:5110/api/boards/$BOARD/tasks/$TASK" -H "Content-Type: application/json" -d "{\"taskName\":\"Правка участником\",\"taskDescription\":\"ок\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$ME}" -o /dev/null -w "put_by_member=%{http_code}\n"
```

Ожидаемо: `adduser=200`, строка `board=… task=…` с числами, `put_by_member=200`.

- [ ] Коммит:

```bash
cd . && git add Services/TaskService.cs && git commit -m "Бек: задачу может редактировать любой активный участник доски"
```

---

## Task 6: Комментарии скоупятся по boardId

**Files:**
- Modify: `./Services/CommentService.cs` (метод `GetAllCommentsOfTaskAsync`, строки 157–178)

- [ ] В `Services/CommentService.cs` заменить начало метода `GetAllCommentsOfTaskAsync`:

```csharp
            if(! await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var comments = await _db.Comments
                .Where(c => c.TaskId == taskId)
```

на:

```csharp
            if(! await IsUserBoardMemberAsync(boardId, userId, ct))
            {
                return null;
            }

            var taskFromThisBoard = await _db.Tasks
                .AnyAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (!taskFromThisBoard)
            {
                return null;
            }

            var comments = await _db.Comments
                .Where(c => c.TaskId == taskId)
```

- [ ] Проверка: перезапустить приложение и убедиться, что чужая задача по перебору `taskId` даёт 404. Скрипт создаёт две доски одним пользователем, задачу во второй доске и пытается прочитать её комментарии через первую доску:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; ME=$(curl -s -b /tmp/kb.cookies http://localhost:5110/api/auth/me | python3 -c "import sys,json;print(json.load(sys.stdin)['userId'])") ; B1=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Доска А","description":"a"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; B2=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Доска Б","description":"b"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; T2=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B2/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"Чужая\",\"taskDescription\":null,\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$ME}" | python3 -c "import sys,json;print(json.load(sys.stdin)['taskId'])") ; echo "b1=$B1 b2=$B2 t2=$T2" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B1/tasks/$T2/comments" -o /dev/null -w "cross_board=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B2/tasks/$T2/comments" -o /dev/null -w "own_board=%{http_code}\n"
```

Ожидаемо: `cross_board=404`, `own_board=200`.

- [ ] Коммит:

```bash
cd . && git add Services/CommentService.cs && git commit -m "Бек: комментарии задачи скоупятся по доске"
```

---

## Task 7: `Author.Login` в списке досок, создании и обновлении

**Files:**
- Modify: `./Services/BoardService.cs` (методы `UpdateBoardAsync` 103–126, `CreateBoardAsync` 167–199, `GetAllUserBoardsAsync` 200–215)

- [ ] В `Services/BoardService.cs` заменить метод `UpdateBoardAsync` целиком:

```csharp
        public async Task<BoardResponse?> UpdateBoardAsync(int boardId, int userId, BoardRequest boardRequest, CancellationToken ct)
        {
            var board = await _db.Boards
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == userId
                                        && b.BoardUsers.Any(bu => bu.UserId == userId && !bu.IsDeleted), ct);


            if (board is null)
                return null;

            board.NameOfBoard = boardRequest.Name;
            board.Description = boardRequest.Description;

            await _db.SaveChangesAsync(ct);

            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse { UserId = board.AuthorId },
                DateOfMade = board.DateOfMade
            };
        }
```

на:

```csharp
        public async Task<BoardResponse?> UpdateBoardAsync(int boardId, int userId, BoardRequest boardRequest, CancellationToken ct)
        {
            var board = await _db.Boards
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BoardId == boardId && b.AuthorId == userId
                                        && b.BoardUsers.Any(bu => bu.UserId == userId && !bu.IsDeleted), ct);


            if (board is null)
                return null;

            board.NameOfBoard = boardRequest.Name;
            board.Description = boardRequest.Description;

            await _db.SaveChangesAsync(ct);

            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse
                {
                    UserId = board.Author.UserId,
                    Login = board.Author.Login
                },
                DateOfMade = board.DateOfMade
            };
        }
```

- [ ] В `Services/BoardService.cs` заменить метод `CreateBoardAsync` целиком:

```csharp
        public async Task<BoardResponse> CreateBoardAsync (int userId, BoardRequest request, CancellationToken ct)
        {
            var board = new Board
            {
                NameOfBoard = request.Name,
                Description = request.Description,
                AuthorId = userId,
                DateOfMade = DateTime.UtcNow
            };
            var boardUser = new BoardUser
            {
                UserId = userId,
                Board = board,
                DateOfJoin = DateTime.UtcNow
            };
            _db.BoardUsers.Add(boardUser);


            await _db.SaveChangesAsync(ct);

            await _statusService.CreateDefaultStatusesAsync(board.BoardId, ct);


            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse { UserId = board.AuthorId },
                DateOfMade = board.DateOfMade
            };

        }
```

на:

```csharp
        public async Task<BoardResponse> CreateBoardAsync (int userId, BoardRequest request, CancellationToken ct)
        {
            var board = new Board
            {
                NameOfBoard = request.Name,
                Description = request.Description,
                AuthorId = userId,
                DateOfMade = DateTime.UtcNow
            };
            var boardUser = new BoardUser
            {
                UserId = userId,
                Board = board,
                DateOfJoin = DateTime.UtcNow
            };
            _db.BoardUsers.Add(boardUser);


            await _db.SaveChangesAsync(ct);

            await _statusService.CreateDefaultStatusesAsync(board.BoardId, ct);

            var author = await _db.Users.FirstAsync(u => u.UserId == userId, ct);

            return new BoardResponse
            {
                BoardId = board.BoardId,
                NameOfBoard = board.NameOfBoard,
                Description = board.Description,
                Author = new UserResponse
                {
                    UserId = author.UserId,
                    Login = author.Login
                },
                DateOfMade = board.DateOfMade
            };

        }
```

- [ ] В `Services/BoardService.cs` заменить проекцию в `GetAllUserBoardsAsync`:

```csharp
                Author = new UserResponse { UserId = b.AuthorId },
                DateOfMade = b.DateOfMade
            })
            .ToListAsync(ct);
```

на:

```csharp
                Author = new UserResponse
                {
                    UserId = b.Author.UserId,
                    Login = b.Author.Login
                },
                DateOfMade = b.DateOfMade
            })
            .ToListAsync(ct);
```

- [ ] Проверка: перезапустить приложение и убедиться, что логин автора приходит во всех трёх ответах:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Автор","description":"проверка логина"}' | python3 -c "import sys,json;d=json.load(sys.stdin);print(d['boardId'],d['author']['login'])") ; echo "create=$B" ; curl -s -b /tmp/kb.cookies http://localhost:5110/api/boards | python3 -c "import sys,json;print('list_logins=',set(b['author']['login'] for b in json.load(sys.stdin)))" ; BID=$(echo $B | cut -d' ' -f1) ; curl -s -b /tmp/kb.cookies -X PUT "http://localhost:5110/api/boards/$BID" -H "Content-Type: application/json" -d '{"name":"Автор 2","description":"проверка логина"}' | python3 -c "import sys,json;print('update_login=',json.load(sys.stdin)['author']['login'])"
```

Ожидаемо: `create=<id> kbtest`, `list_logins= {'kbtest'}` (без `None`), `update_login= kbtest`.

- [ ] Коммит:

```bash
cd . && git add Services/BoardService.cs && git commit -m "Бек: логин автора доски заполняется в списке, создании и обновлении"
```

---

## Task 8: `AttachmentResponse` без `FilePath`

**Files:**
- Modify: `./Models/Responses/AttachmentResponse.cs`
- Modify: `./Services/AttachmentService.cs` (строки 119, 143, 240)

- [ ] Перезаписать `Models/Responses/AttachmentResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class AttachmentResponse
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public DateTime DateOfUpload { get; set; }
        public UserResponse Uploader { get; set; } = null!;
    }
}
```

- [ ] В `Services/AttachmentService.cs` в методе `GetAllCommentsAttachments` заменить:

```csharp
                    AttachmentId = a.AttachmentId,
                    DateOfUpload = a.DateOfUpload,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    Uploader = new UserResponse
                    {
                        Login = a.Uploader.User.Login,
                        UserId = a.Uploader.UserId
                    },
                })
                .ToListAsync(ct);

            return attachments;
        }

        public async Task<List<AttachmentResponse>?> GetAllTasksAttachments(int boardId, int userId, int taskId, CancellationToken ct)
```

на:

```csharp
                    AttachmentId = a.AttachmentId,
                    DateOfUpload = a.DateOfUpload,
                    FileName = a.FileName,
                    Uploader = new UserResponse
                    {
                        Login = a.Uploader.User.Login,
                        UserId = a.Uploader.UserId
                    },
                })
                .ToListAsync(ct);

            return attachments;
        }

        public async Task<List<AttachmentResponse>?> GetAllTasksAttachments(int boardId, int userId, int taskId, CancellationToken ct)
```

- [ ] В `Services/AttachmentService.cs` в методе `GetAllTasksAttachments` заменить:

```csharp
                .Select(a => new AttachmentResponse 
                {
                    AttachmentId = a.AttachmentId,
                    DateOfUpload = a.DateOfUpload,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
```

на:

```csharp
                .Select(a => new AttachmentResponse 
                {
                    AttachmentId = a.AttachmentId,
                    DateOfUpload = a.DateOfUpload,
                    FileName = a.FileName,
```

- [ ] В `Services/AttachmentService.cs` в методе `SaveAttachmentAsync` заменить:

```csharp
            return new AttachmentResponse
            {
                AttachmentId = attachment.AttachmentId,
                DateOfUpload = attachment.DateOfUpload,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                Uploader = new UserResponse
```

на:

```csharp
            return new AttachmentResponse
            {
                AttachmentId = attachment.AttachmentId,
                DateOfUpload = attachment.DateOfUpload,
                FileName = attachment.FileName,
                Uploader = new UserResponse
```

- [ ] Проверка: перезапустить приложение, загрузить файл в задачу и убедиться, что `filePath` не приходит:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && echo "привет" > /tmp/kb-file.txt && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; ME=$(curl -s -b /tmp/kb.cookies http://localhost:5110/api/auth/me | python3 -c "import sys,json;print(json.load(sys.stdin)['userId'])") ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Вложения","description":"проверка"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; T=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"Файл\",\"taskDescription\":null,\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$ME}" | python3 -c "import sys,json;print(json.load(sys.stdin)['taskId'])") ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks/$T/attachments" -F "file=@/tmp/kb-file.txt" -w "\nupload=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$T/attachments"
```

Ожидаемо: `upload=200`; в теле ответа на загрузку и в списке есть `attachmentId`, `fileName`, `dateOfUpload`, `uploader` и НЕТ ключа `filePath`.

- [ ] Коммит:

```bash
cd . && git add Models/Responses/AttachmentResponse.cs Services/AttachmentService.cs && git commit -m "Бек: AttachmentResponse больше не отдаёт серверный путь к файлу"
```

---

# ФАЗА 2 — ORDERING И НОВЫЕ ЭНДПОИНТЫ

## Task 9: Поля `Order` и миграция `AddOrdering`

**Files:**
- Modify: `./Models/Status.cs`
- Modify: `./Models/Task.cs`
- Create: `./Migrations/<timestamp>_AddOrdering.cs` (+ `.Designer.cs`, генерируются `dotnet ef`)
- Modify: `./Migrations/KanbanBoardDbContextModelSnapshot.cs` (обновляется `dotnet ef`)

- [ ] Перезаписать `Models/Status.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Status
    {
        [Key] 
        public int StatusId { get; set; }
        [MaxLength(100)]
        public string StatusName { get; set; } = null!;
        [ForeignKey("Board")]
        public int BoardId { get; set; }
        public int Order { get; set; }


        public Board Board { get; set; } = null!;
        [InverseProperty("Status")]
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        [InverseProperty("Status")]
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}
```

- [ ] Перезаписать `Models/Task.cs` (добавлен `Order`, навигация `Assignee` стала nullable — FK `AssigneeId` уже `int?`, поэтому схема БД не меняется):

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoard.Models
{
    public class Task
    {
        [Key]
        public int TaskId { get; set; }
        [MaxLength(50)]
        public string TaskName { get; set; } = null!;
        [MaxLength(3000)]
        public string? TaskDescription { get; set; }
        public int? AssigneeId { get; set; }
        public int AuthorId { get; set; }

        [ForeignKey("Status")]
        public int StatusId { get; set; }

        [ForeignKey("Board")]
        public int BoardId { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime DeadLine { get; set; }
        public int Order { get; set; }

        public BoardUser Author { get; set; } = null!;

        public BoardUser? Assignee { get; set; }

        
        public Board Board { get; set; } = null!;

        public Status Status { get; set; } = null!;

        [InverseProperty("Task")]
        public ICollection<Comment> Comments {  get; set; } = new List<Comment> ();
        [InverseProperty("Task")]
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        [InverseProperty("Task")]
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
    }
}
```

- [ ] Убедиться, что установлен инструмент `dotnet-ef`:

```bash
dotnet ef --version || dotnet tool install --global dotnet-ef
```

Ожидаемо: строка с версией (например `Entity Framework Core .NET Command-line Tools 9.x`). Если инструмента не было — установка и затем повторная проверка версии.

- [ ] Остановить приложение и сгенерировать миграцию (файлы `.cs` и `.Designer.cs` пишет EF, руками их не создаём):

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet ef migrations add AddOrdering 2>&1 | tail -10
```

Ожидаемо: `Build started...`, `Build succeeded.`, `Done. To undo this action, use 'ef migrations remove'`. Появились файлы `Migrations/<timestamp>_AddOrdering.cs` и `Migrations/<timestamp>_AddOrdering.Designer.cs`.

- [ ] Найти сгенерированный файл и прочитать его:

```bash
cd . && ls Migrations/*AddOrdering.cs && cat Migrations/*_AddOrdering.cs
```

Ожидаемо: в `Up()` два вызова `migrationBuilder.AddColumn<int>` — для таблицы `Statuses` и `Tasks`, оба с `name: "Order"`, `nullable: false`, `defaultValue: 0`; в `Down()` — два `DropColumn`. Если `AddColumn` только один или имена колонок другие — остановиться и разобраться (модели не сохранились / миграция взяла старый снапшот).

- [ ] Дописать бекфилл в `Up()` сгенерированного файла: сразу после двух вызовов `AddColumn` (перед закрывающей скобкой метода `Up`) добавить:

```csharp
            migrationBuilder.Sql(@"
                WITH ordered AS (
                    SELECT ""StatusId"", ROW_NUMBER() OVER (PARTITION BY ""BoardId"" ORDER BY ""StatusId"") - 1 AS rn
                    FROM ""Statuses""
                )
                UPDATE ""Statuses"" s
                SET ""Order"" = ordered.rn
                FROM ordered
                WHERE s.""StatusId"" = ordered.""StatusId"";
            ");

            migrationBuilder.Sql(@"
                WITH ordered AS (
                    SELECT ""TaskId"", ROW_NUMBER() OVER (PARTITION BY ""StatusId"" ORDER BY ""CreationDate"", ""TaskId"") - 1 AS rn
                    FROM ""Tasks""
                )
                UPDATE ""Tasks"" t
                SET ""Order"" = ordered.rn
                FROM ordered
                WHERE t.""TaskId"" = ordered.""TaskId"";
            ");
```

`Down()` не трогаем: `DropColumn` откатывает всё.

- [ ] Применить миграцию:

```bash
cd . && dotnet ef database update 2>&1 | tail -10
```

Ожидаемо: `Applying migration '<timestamp>_AddOrdering'.` и `Done.`

- [ ] Проверка: перезапустить приложение и убедиться, что колонки существующей доски получили разные `Order` (пока сериализуется как поле сущности только внутри бека — проверяем через список задач и колонок после Task 10; сейчас достаточно, что приложение стартует и читает данные):

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; curl -s -b /tmp/kb.cookies http://localhost:5110/api/boards | python3 -c "import sys,json;d=json.load(sys.stdin);print('boards=',len(d))"
```

Ожидаемо: `login=200`, `boards=` число больше нуля, приложение не падает.

- [ ] Коммит:

```bash
cd . && git add Models/Status.cs Models/Task.cs Migrations && git commit -m "Бек: миграция AddOrdering — порядок колонок и карточек с бекфиллом"
```

---

## Task 10: `StatusResponse.Order`, сортировка колонок и дефолтные колонки

**Files:**
- Modify: `./Models/Responses/StatusResponse.cs`
- Modify: `./Services/StatusService.cs` (методы `GetBoardStatusesAsync`, `CreateDefaultStatusesAsync`)

- [ ] Перезаписать `Models/Responses/StatusResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class StatusResponse
    {
        public int StatusId {  get; set; }
        public string StatusName { get; set; } = null!;
        public int Order { get; set; }
    }
}
```

- [ ] В `Services/StatusService.cs` заменить тело выборки в `GetBoardStatusesAsync`:

```csharp
            var boardStatuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .Select(s => new StatusResponse
                {
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                })
                .ToListAsync(ct);
```

на:

```csharp
            var boardStatuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .Select(s => new StatusResponse
                {
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                    Order = s.Order,
                })
                .ToListAsync(ct);
```

- [ ] В `Services/StatusService.cs` заменить список дефолтных колонок в `CreateDefaultStatusesAsync`:

```csharp
            var newStatuses = new List<Status>
            {
                new Status { BoardId = boardId, StatusName = "To Do" },
                new Status { BoardId = boardId, StatusName = "In Progress" },
                new Status { BoardId = boardId, StatusName = "Done" },
            };
```

на:

```csharp
            var newStatuses = new List<Status>
            {
                new Status { BoardId = boardId, StatusName = "To Do", Order = 0 },
                new Status { BoardId = boardId, StatusName = "In Progress", Order = 1 },
                new Status { BoardId = boardId, StatusName = "Done", Order = 2 },
            };
```

- [ ] Проверка: перезапустить приложение, создать доску и посмотреть колонки:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Колонки","description":"порядок"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; echo "board=$B" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses"
```

Ожидаемо: тело вида `[{"statusId":N,"statusName":"To Do","order":0},{"statusId":N+1,"statusName":"In Progress","order":1},{"statusId":N+2,"statusName":"Done","order":2}]`.

- [ ] Коммит:

```bash
cd . && git add Models/Responses/StatusResponse.cs Services/StatusService.cs && git commit -m "Бек: колонки отдают порядок и сортируются по (Order, StatusId)"
```

---

## Task 11: Итоговая форма `TaskResponse` и общая проекция задач

**Files:**
- Modify: `./Models/Responses/TaskResponse.cs`
- Modify: `./Services/TaskService.cs` (шапка файла, метод `GetAllBoardTasksAsync`)

- [ ] Перезаписать `Models/Responses/TaskResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class TaskResponse
    {
        public int TaskId { get; set; }
        public int BoardId { get; set; }
        public string TaskName { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime DateOfMade { get; set; }
        public int Order { get; set; }
        public StatusResponse Status { get; set; } = null!;
        public UserResponse? Worker { get; set; }
        public UserResponse Author { get; set; } = null!;
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
    }
}
```

- [ ] В `Services/TaskService.cs` заменить шапку файла и начало класса (добавляются `using System.Linq.Expressions;`, общая проекция `ToTaskResponse` и хелпер `GetTaskResponseAsync`, который используется всеми одиночными путями). Заменить:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Task = KanbanBoard.Models.Task;
namespace KanbanBoard.Services
{
    public class TaskService
    {
        private readonly KanbanBoardDbContext _db;

        public TaskService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }
```

на:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Task = KanbanBoard.Models.Task;
namespace KanbanBoard.Services
{
    public class TaskService
    {
        private readonly KanbanBoardDbContext _db;

        public TaskService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private static readonly Expression<Func<Task, TaskResponse>> ToTaskResponse = t => new TaskResponse
        {
            TaskId = t.TaskId,
            BoardId = t.BoardId,
            TaskName = t.TaskName,
            TaskDescription = t.TaskDescription,
            Deadline = t.DeadLine,
            DateOfMade = t.CreationDate,
            Order = t.Order,
            Status = new StatusResponse
            {
                StatusId = t.Status.StatusId,
                StatusName = t.Status.StatusName,
                Order = t.Status.Order
            },
            Worker = t.Assignee == null ? null : new UserResponse
            {
                UserId = t.Assignee.UserId,
                Login = t.Assignee.User.Login
            },
            Author = new UserResponse
            {
                UserId = t.Author.UserId,
                Login = t.Author.User.Login
            },
            CommentsCount = t.Comments.Count,
            AttachmentsCount = t.Attachments.Count
        };

        private async Task<TaskResponse?> GetTaskResponseAsync(int boardId, int taskId, CancellationToken ct)
        {
            return await _db.Tasks
                .Where(t => t.BoardId == boardId && t.TaskId == taskId)
                .Select(ToTaskResponse)
                .FirstOrDefaultAsync(ct);
        }
```

- [ ] В `Services/TaskService.cs` заменить хвост метода `GetAllBoardTasksAsync` (вся ручная проекция уходит в общую):

```csharp
            var tasks = await query
                .Select(t => new TaskResponse
                {
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    Deadline = t.DeadLine,
                    DateOfMade = t.CreationDate,
                    Status = new StatusResponse
                    {
                        StatusId = t.Status.StatusId,
                        StatusName = t.Status.StatusName
                    },
                    Worker = new UserResponse
                    {
                        UserId = t.Assignee.UserId,
                        Login = t.Assignee.User.Login,
                    },
                    Author = new UserResponse
                    {
                        UserId = t.Author.UserId,
                        Login = t.Author.User.Login,
                    }
                })
                .ToListAsync(ct);

            return tasks;
```

на:

```csharp
            var tasks = await query
                .OrderBy(t => t.Order)
                .ThenBy(t => t.TaskId)
                .Select(ToTaskResponse)
                .ToListAsync(ct);

            return tasks;
```

- [ ] Проверка сборки (в `CreateTaskAsync`/`UpdateTaskAsync` ещё осталась старая ручная проекция — она использует `Worker = new UserResponse {...}`, что по-прежнему компилируется, потому что `Worker` теперь `UserResponse?`):

```bash
cd . && dotnet build 2>&1 | tail -5
```

Ожидаемо: `Build succeeded.`, `0 Error(s)`.

- [ ] Проверка списка задач: перезапустить приложение и посмотреть форму ответа:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; ME=$(curl -s -b /tmp/kb.cookies http://localhost:5110/api/auth/me | python3 -c "import sys,json;print(json.load(sys.stdin)['userId'])") ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Форма задачи","description":"dto"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"Первая\",\"taskDescription\":\"описание\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$ME}" -o /dev/null -w "create=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks"
```

Ожидаемо: `create=200`; в списке один объект, у него есть ключи `taskId`, `boardId`, `taskName`, `taskDescription`, `deadline`, `dateOfMade`, `order`, `status` (с `order`), `worker`, `author`, `commentsCount` (=0), `attachmentsCount` (=0).

- [ ] Коммит:

```bash
cd . && git add Models/Responses/TaskResponse.cs Services/TaskService.cs && git commit -m "Бек: итоговая форма TaskResponse и общая проекция задач с сортировкой"
```

---

## Task 12: Создание и обновление задачи — `WorkerId`, опциональный исполнитель, `Order`, первая строка истории

**Files:**
- Modify: `./Models/Requests/TaskRequest.cs`
- Modify: `./Services/TaskService.cs` (методы `CreateTaskAsync`, `UpdateTaskAsync`)

- [ ] Перезаписать `Models/Requests/TaskRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class TaskRequest
    {
        [Required]
        [MaxLength(50)]
        public string TaskName { get; set; } = null!;

        [MaxLength(3000)]
        public string? TaskDescription { get; set; }

        public DateTime Deadline { get; set; }

        public int? WorkerId { get; set; }
    }
}
```

- [ ] В `Services/TaskService.cs` заменить метод `CreateTaskAsync` целиком (от строки `public async Task<TaskResponse?> CreateTaskAsync(` до её закрывающей скобки) на:

```csharp
        public async Task<TaskResponse?> CreateTaskAsync(int boardId, int userId, TaskRequest request,  CancellationToken ct)
        {
            var authorFromThisBoard = await _db.BoardUsers
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);

            if (authorFromThisBoard == null)
                return null;

            BoardUser? workerFromThisBoard = null;

            if (request.WorkerId.HasValue)
            {
                workerFromThisBoard = await _db.BoardUsers
                    .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == request.WorkerId.Value && !bu.IsDeleted, ct);

                if (workerFromThisBoard == null)
                    return null;
            }

            var defaultStatus = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .FirstOrDefaultAsync(ct);

            if (defaultStatus == null)
            {
                return null;
            }

            var maxOrder = await _db.Tasks
                .Where(t => t.StatusId == defaultStatus.StatusId)
                .MaxAsync(t => (int?)t.Order, ct) ?? -1;

            var task = new Task
            {
                TaskName = request.TaskName,
                TaskDescription = request.TaskDescription,
                AssigneeId = workerFromThisBoard?.BoardUserId,
                AuthorId = authorFromThisBoard.BoardUserId,
                BoardId = boardId,
                StatusId = defaultStatus.StatusId,
                Order = maxOrder + 1,
                DeadLine = request.Deadline,
                CreationDate = DateTime.UtcNow,
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync(ct);

            var history = new TaskStatusHistory
            {
                TaskId = task.TaskId,
                StatusId = defaultStatus.StatusId,
                AuthorId = authorFromThisBoard.BoardUserId,
                ChangeDate = task.CreationDate
            };

            _db.TaskStatusHistories.Add(history);
            await _db.SaveChangesAsync(ct);

            return await GetTaskResponseAsync(boardId, task.TaskId, ct);
        }
```

- [ ] В `Services/TaskService.cs` заменить метод `UpdateTaskAsync` целиком (от строки `public async Task<TaskResponse?> UpdateTaskAsync(` до её закрывающей скобки) на:

```csharp
        public async Task<TaskResponse?> UpdateTaskAsync(int boardId, int userId, int taskId, TaskRequest request, CancellationToken ct)
        {
            var currentBoardUser = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (currentBoardUser == null)
                return null;

            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (task == null)
                return null;

            int? assigneeId = null;

            if (request.WorkerId.HasValue)
            {
                var workerFromThisBoard = await _db.BoardUsers
                    .FirstOrDefaultAsync(bu => bu.UserId == request.WorkerId.Value && bu.BoardId == boardId && !bu.IsDeleted, ct);
                if (workerFromThisBoard == null)
                    return null;

                assigneeId = workerFromThisBoard.BoardUserId;
            }

            task.TaskName = request.TaskName;
            task.TaskDescription = request.TaskDescription;
            task.DeadLine = request.Deadline;
            task.AssigneeId = assigneeId;

            await _db.SaveChangesAsync(ct);

            return await GetTaskResponseAsync(boardId, taskId, ct);
        }
```

- [ ] Проверка: перезапустить приложение и прогнать четыре сценария — задача с исполнителем, задача без исполнителя, снятие исполнителя через PUT, чужой `WorkerId` → 404:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies /tmp/kb2.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; curl -s -c /tmp/kb2.cookies -X POST http://localhost:5110/api/auth/register -H "Content-Type: application/json" -d '{"login":"kbtest2","password":"secret123"}' -o /dev/null ; ME=$(curl -s -b /tmp/kb.cookies http://localhost:5110/api/auth/me | python3 -c "import sys,json;print(json.load(sys.stdin)['userId'])") ; curl -s -c /tmp/kb2.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest2","password":"secret123"}' -o /dev/null ; OTHER=$(curl -s -b /tmp/kb2.cookies http://localhost:5110/api/auth/me | python3 -c "import sys,json;print(json.load(sys.stdin)['userId'])") ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Исполнители","description":"проверка"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; echo "board=$B me=$ME other=$OTHER" ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"С исполнителем\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$ME}" | python3 -c "import sys,json;d=json.load(sys.stdin);print('with_worker order=',d['order'],'worker=',d['worker'])" ; T=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d '{"taskName":"Без исполнителя","deadline":"2026-12-31T12:00:00Z","workerId":null}' | python3 -c "import sys,json;d=json.load(sys.stdin);print(d['taskId'],'no_worker order=',d['order'],'worker=',d['worker'],file=sys.stderr);print(d['taskId'])") ; curl -s -b /tmp/kb.cookies -X PUT "http://localhost:5110/api/boards/$B/tasks/$T" -H "Content-Type: application/json" -d "{\"taskName\":\"Снимаем исполнителя\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":null}" | python3 -c "import sys,json;print('put_null_worker=',json.load(sys.stdin)['worker'])" ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"Чужой исполнитель\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":$OTHER}" -o /dev/null -w "alien_worker=%{http_code}\n"
```

Ожидаемо: `with_worker order= 0 worker= {'userId': …, 'login': 'kbtest'}`, во второй строке (stderr) `no_worker order= 1 worker= None`, `put_null_worker= None`, `alien_worker=404`.

- [ ] Проверка первой строки истории (она понадобится в Task 14, но пишется уже здесь) — история пока читается только через прямой запрос к БД, поэтому проверим её после Task 14. Сейчас достаточно убедиться, что создание задачи не падает: в предыдущем шаге все создания вернули 200.

- [ ] Коммит:

```bash
cd . && git add Models/Requests/TaskRequest.cs Services/TaskService.cs && git commit -m "Бек: задача создаётся с исполнителем из запроса, опциональным исполнителем, порядком и первой строкой истории"
```

---

## Task 13: `GET /api/boards/{boardId}/tasks/{taskId}`

**Files:**
- Modify: `./Services/TaskService.cs` (новый метод `GetBoardTaskAsync`)
- Modify: `./Controllers/TaskController.cs` (новый экшен)

- [ ] В `Services/TaskService.cs` добавить метод сразу перед `GetAllBoardTasksAsync`. Заменить:

```csharp
        public async Task<List<TaskResponse>?> GetAllBoardTasksAsync(int boardId, int userId,  CancellationToken ct, int? statusId = null,  string? search = null)
```

на:

```csharp
        public async Task<TaskResponse?> GetBoardTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            return await GetTaskResponseAsync(boardId, taskId, ct);
        }

        public async Task<List<TaskResponse>?> GetAllBoardTasksAsync(int boardId, int userId,  CancellationToken ct, int? statusId = null,  string? search = null)
```

- [ ] В `Controllers/TaskController.cs` добавить экшен сразу после `GetAllBoardTasksAsync`. Заменить:

```csharp
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks")]
        public async Task<IActionResult> AddNewTask(int boardId, [FromBody] TaskRequest request, CancellationToken ct)
```

на:

```csharp
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}")]
        public async Task<IActionResult> GetBoardTask(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.GetBoardTaskAsync(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks")]
        public async Task<IActionResult> AddNewTask(int boardId, [FromBody] TaskRequest request, CancellationToken ct)
```

- [ ] Проверка: перезапустить приложение и прогнать три сценария — своя задача, чужая доска (скоупинг по `boardId`), неавторизованный:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B1=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"GET A","description":"a"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; B2=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"GET Б","description":"b"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; T=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B2/tasks" -H "Content-Type: application/json" -d '{"taskName":"Одна задача","deadline":"2026-12-31T12:00:00Z","workerId":null}' | python3 -c "import sys,json;print(json.load(sys.stdin)['taskId'])") ; echo "b1=$B1 b2=$B2 t=$T" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B2/tasks/$T" -w "\nown=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B1/tasks/$T" -o /dev/null -w "cross_board=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B2/tasks/99999999" -o /dev/null -w "missing=%{http_code}\n" ; curl -s "http://localhost:5110/api/boards/$B2/tasks/$T" -o /dev/null -w "anon=%{http_code}\n"
```

Ожидаемо: `own=200` и в теле объект `TaskResponse` с `"worker":null`, `cross_board=404`, `missing=404`, `anon=401`.

- [ ] Коммит:

```bash
cd . && git add Services/TaskService.cs Controllers/TaskController.cs && git commit -m "Бек: GET одной задачи со скоупингом по доске"
```

---

## Task 14: История статусов — `GET .../history` и порядок в старом `PATCH .../status`

**Files:**
- Modify: `./Models/Responses/StatusHistoryResponse.cs`
- Modify: `./Services/TaskService.cs` (метод `ChangeTaskStatusAsync`, новый метод `GetTaskHistoryAsync`)
- Modify: `./Controllers/TaskController.cs` (новый экшен)

- [ ] Перезаписать `Models/Responses/StatusHistoryResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class StatusHistoryResponse
    {
        public int StatusChangeId { get; set; }
        public int TaskId { get; set; }
        public StatusResponse Status { get; set; } = null!;
        public DateTime LastStatusChangeDate { get; set; }
        public UserResponse Author { get; set; } = null!;
    }
}
```

- [ ] В `Services/TaskService.cs` заменить метод `ChangeTaskStatusAsync` целиком (от строки `public async Task<StatusHistoryResponse?> ChangeTaskStatusAsync(` до её закрывающей скобки) на:

```csharp
        public async Task<StatusHistoryResponse?> ChangeTaskStatusAsync(int boardId, int userId, int taskId, StatusHistoryRequest request ,CancellationToken ct)
        {
            var workerFromThisBoard = await _db.BoardUsers
            .Include(bu => bu.User)
            .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);

            if (workerFromThisBoard == null)
                return null;

            var changedTaskByStatus = await _db.Tasks
            .FirstOrDefaultAsync(t => t.BoardId == boardId && t.TaskId == taskId, ct);

            var newStatus = await _db.Statuses
                .Where(s => s.StatusId == request.NewStatusId && s.BoardId == boardId)
                .FirstOrDefaultAsync(ct);

            if (changedTaskByStatus == null || newStatus == null)
            {
                return null;
            }

            var maxOrder = await _db.Tasks
                .Where(t => t.StatusId == newStatus.StatusId && t.TaskId != changedTaskByStatus.TaskId)
                .MaxAsync(t => (int?)t.Order, ct) ?? -1;

            changedTaskByStatus.StatusId = newStatus.StatusId;
            changedTaskByStatus.Order = maxOrder + 1;

            var history = new TaskStatusHistory
            {
                TaskId = changedTaskByStatus.TaskId,
                StatusId = newStatus.StatusId,
                AuthorId = workerFromThisBoard.BoardUserId,
                ChangeDate = DateTime.UtcNow
            };
            _db.TaskStatusHistories.Add(history);
            await _db.SaveChangesAsync(ct);

            return new StatusHistoryResponse
            {
                StatusChangeId = history.StatusChangeId,
                TaskId = changedTaskByStatus.TaskId,
                Status = new StatusResponse
                {
                    StatusId = newStatus.StatusId,
                    StatusName = newStatus.StatusName,
                    Order = newStatus.Order
                },
                LastStatusChangeDate = history.ChangeDate,
                Author = new UserResponse
                {
                    UserId = workerFromThisBoard.UserId,
                    Login = workerFromThisBoard.User.Login
                }
            };
        }
```

- [ ] В `Services/TaskService.cs` добавить чтение истории сразу перед `GetBoardTaskAsync`. Заменить:

```csharp
        public async Task<TaskResponse?> GetBoardTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
```

на:

```csharp
        public async Task<List<StatusHistoryResponse>?> GetTaskHistoryAsync(int boardId, int userId, int taskId, CancellationToken ct)
        {
            if (!await IsUserBoardMemberAsync(boardId, userId, ct))
                return null;

            var taskFromThisBoard = await _db.Tasks
                .AnyAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);

            if (!taskFromThisBoard)
                return null;

            return await _db.TaskStatusHistories
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.ChangeDate)
                .ThenByDescending(h => h.StatusChangeId)
                .Select(h => new StatusHistoryResponse
                {
                    StatusChangeId = h.StatusChangeId,
                    TaskId = h.TaskId,
                    Status = new StatusResponse
                    {
                        StatusId = h.Status.StatusId,
                        StatusName = h.Status.StatusName,
                        Order = h.Status.Order
                    },
                    LastStatusChangeDate = h.ChangeDate,
                    Author = new UserResponse
                    {
                        UserId = h.Author.UserId,
                        Login = h.Author.User.Login
                    }
                })
                .ToListAsync(ct);
        }

        public async Task<TaskResponse?> GetBoardTaskAsync(int boardId, int userId, int taskId, CancellationToken ct)
```

- [ ] В `Controllers/TaskController.cs` добавить экшен истории сразу после экшена `GetBoardTask`. Заменить:

```csharp
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks")]
        public async Task<IActionResult> AddNewTask(int boardId, [FromBody] TaskRequest request, CancellationToken ct)
```

на:

```csharp
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/history")]
        public async Task<IActionResult> GetTaskHistory(int boardId, int taskId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.GetTaskHistoryAsync(boardId, userId, taskId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks")]
        public async Task<IActionResult> AddNewTask(int boardId, [FromBody] TaskRequest request, CancellationToken ct)
```

- [ ] Проверка: перезапустить приложение, создать задачу (первая строка истории), перевести её в другую колонку старым `PATCH .../status` и прочитать историю:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"История","description":"h"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; S2=$(curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print(json.load(sys.stdin)[1]['statusId'])") ; T=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d '{"taskName":"История задачи","deadline":"2026-12-31T12:00:00Z","workerId":null}' | python3 -c "import sys,json;print(json.load(sys.stdin)['taskId'])") ; echo "board=$B status2=$S2 task=$T" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$T/history" | python3 -c "import sys,json;d=json.load(sys.stdin);print('history_after_create=',len(d),d[0]['status']['statusName'],d[0]['author']['login'])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/tasks/$T/status" -H "Content-Type: application/json" -d "{\"newStatusId\":$S2}" -w "\npatch=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$T/history" | python3 -c "import sys,json;d=json.load(sys.stdin);print('history_after_patch=',len(d),[h['status']['statusName'] for h in d])" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$T" | python3 -c "import sys,json;d=json.load(sys.stdin);print('task_after_patch status=',d['status']['statusName'],'order=',d['order'])"
```

Ожидаемо: `history_after_create= 1 To Do kbtest`; `patch=200` и в теле объект с `statusChangeId`, `author`; `history_after_patch= 2 ['In Progress', 'To Do']` (свежие сверху); `task_after_patch status= In Progress order= 0`.

- [ ] Коммит:

```bash
cd . && git add Models/Responses/StatusHistoryResponse.cs Services/TaskService.cs Controllers/TaskController.cs && git commit -m "Бек: история статусов задачи с автором и порядок при старой смене статуса"
```

---

## Task 15: CRUD колонок

**Files:**
- Create: `./Models/Requests/StatusRequest.cs`
- Modify: `./Services/StatusService.cs`
- Modify: `./Controllers/StatusController.cs`

- [ ] Создать `Models/Requests/StatusRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace KanbanBoard.Models.Requests
{
    public class StatusRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
    }
}
```

- [ ] В `Services/StatusService.cs` заменить шапку файла и начало класса (добавляются `using` для реквестов, перечисление результата удаления и проверка владельца). Заменить:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public class StatusService
    {
        private readonly KanbanBoardDbContext _db;

        public StatusService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
        }
```

на:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoard.Services
{
    public enum DeleteStatusResult
    {
        NotFound,
        LastStatus,
        HasTasks,
        Deleted
    }

    public class StatusService
    {
        private readonly KanbanBoardDbContext _db;

        public StatusService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private async Task<bool> IsUserBoardMemberAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.BoardUsers.AnyAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
        }

        private async Task<bool> IsUserBoardOwnerAsync(int boardId, int userId, CancellationToken ct)
        {
            return await _db.Boards.AnyAsync(b => b.BoardId == boardId && b.AuthorId == userId, ct);
        }
```

- [ ] В `Services/StatusService.cs` добавить три метода перед `CreateDefaultStatusesAsync`. Заменить:

```csharp
        public async System.Threading.Tasks.Task CreateDefaultStatusesAsync(int boardId, CancellationToken ct)
```

на:

```csharp
        public async Task<StatusResponse?> CreateStatusAsync(int boardId, int userId, StatusRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return null;

            var maxOrder = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .MaxAsync(s => (int?)s.Order, ct) ?? -1;

            var status = new Status
            {
                BoardId = boardId,
                StatusName = request.Name,
                Order = maxOrder + 1
            };

            _db.Statuses.Add(status);
            await _db.SaveChangesAsync(ct);

            return new StatusResponse
            {
                StatusId = status.StatusId,
                StatusName = status.StatusName,
                Order = status.Order
            };
        }

        public async Task<StatusResponse?> UpdateStatusAsync(int boardId, int userId, int statusId, StatusRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return null;

            var status = await _db.Statuses
                .FirstOrDefaultAsync(s => s.StatusId == statusId && s.BoardId == boardId, ct);

            if (status == null)
                return null;

            status.StatusName = request.Name;

            await _db.SaveChangesAsync(ct);

            return new StatusResponse
            {
                StatusId = status.StatusId,
                StatusName = status.StatusName,
                Order = status.Order
            };
        }

        public async Task<DeleteStatusResult> DeleteStatusAsync(int boardId, int userId, int statusId, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return DeleteStatusResult.NotFound;

            var status = await _db.Statuses
                .FirstOrDefaultAsync(s => s.StatusId == statusId && s.BoardId == boardId, ct);

            if (status == null)
                return DeleteStatusResult.NotFound;

            var statusesCount = await _db.Statuses.CountAsync(s => s.BoardId == boardId, ct);
            if (statusesCount <= 1)
                return DeleteStatusResult.LastStatus;

            var hasTasks = await _db.Tasks.AnyAsync(t => t.StatusId == statusId, ct);
            if (hasTasks)
                return DeleteStatusResult.HasTasks;

            _db.Statuses.Remove(status);
            await _db.SaveChangesAsync(ct);

            var restStatuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .ToListAsync(ct);

            for (var i = 0; i < restStatuses.Count; i++)
                restStatuses[i].Order = i;

            await _db.SaveChangesAsync(ct);

            return DeleteStatusResult.Deleted;
        }

        public async System.Threading.Tasks.Task CreateDefaultStatusesAsync(int boardId, CancellationToken ct)
```

- [ ] В `Controllers/StatusController.cs` добавить `using KanbanBoard.Models.Requests;` и три экшена. Заменить весь файл на:

```csharp
using KanbanBoard.Models.Requests;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanbanBoard.Controllers
{
    [ApiController]
    [Authorize]
    public class StatusController(StatusService statusService) : Controller
    {
        private readonly StatusService _statusService = statusService;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/statuses")]
        public async Task<IActionResult> GetBoardStatuses(int boardId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.GetBoardStatusesAsync(boardId, userId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/statuses")]
        public async Task<IActionResult> CreateBoardStatus(int boardId, [FromBody] StatusRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.CreateStatusAsync(boardId, userId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/statuses/{statusId}")]
        public async Task<IActionResult> UpdateBoardStatus(int boardId, int statusId, [FromBody] StatusRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.UpdateStatusAsync(boardId, userId, statusId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("api/boards/{boardId}/statuses/{statusId}")]
        public async Task<IActionResult> DeleteBoardStatus(int boardId, int statusId, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.DeleteStatusAsync(boardId, userId, statusId, ct);

            if (result == DeleteStatusResult.NotFound)
                return NotFound();

            if (result == DeleteStatusResult.HasTasks)
                return Conflict("В колонке есть задачи");

            if (result == DeleteStatusResult.LastStatus)
                return Conflict("Нельзя удалить последнюю колонку");

            return NoContent();
        }
    }
}
```

- [ ] Проверка: перезапустить приложение и прогнать создание, переименование, удаление непустой колонки (409), удаление пустой (204 + переиндексация), удаление от участника-не-владельца (404):

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies /tmp/kb2.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; curl -s -c /tmp/kb2.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest2","password":"secret123"}' -o /dev/null ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"CRUD колонок","description":"c"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/users" -H "Content-Type: application/json" -d '{"login":"kbtest2"}' -o /dev/null ; NEW=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/statuses" -H "Content-Type: application/json" -d '{"name":"Ревью"}' | python3 -c "import sys,json;d=json.load(sys.stdin);print(d['statusId'],'created order=',d['order'],file=sys.stderr);print(d['statusId'])") ; curl -s -b /tmp/kb.cookies -X PUT "http://localhost:5110/api/boards/$B/statuses/$NEW" -H "Content-Type: application/json" -d '{"name":"Код-ревью"}' -w "\nrename=%{http_code}\n" ; FIRST=$(curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print(json.load(sys.stdin)[0]['statusId'])") ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d '{"taskName":"Занимает колонку","deadline":"2026-12-31T12:00:00Z","workerId":null}' -o /dev/null ; curl -s -b /tmp/kb.cookies -X DELETE "http://localhost:5110/api/boards/$B/statuses/$FIRST" -w "\ndelete_nonempty=%{http_code}\n" ; curl -s -b /tmp/kb2.cookies -X DELETE "http://localhost:5110/api/boards/$B/statuses/$NEW" -o /dev/null -w "delete_by_member=%{http_code}\n" ; curl -s -b /tmp/kb.cookies -X DELETE "http://localhost:5110/api/boards/$B/statuses/$NEW" -o /dev/null -w "delete_empty=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print('orders=',[(s['statusName'],s['order']) for s in json.load(sys.stdin)])"
```

Ожидаемо: во второй строке (stderr) `created order= 3`; `rename=200` и в теле `"statusName":"Код-ревью"`; `delete_nonempty=409` с телом `В колонке есть задачи`; `delete_by_member=404`; `delete_empty=204`; `orders= [('To Do', 0), ('In Progress', 1), ('Done', 2)]`.

- [ ] Проверка «последняя колонка»: создать доску, удалить две колонки из трёх, третью удалить не дать:

```bash
B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Последняя колонка","description":"l"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; IDS=$(curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print(' '.join(str(s['statusId']) for s in json.load(sys.stdin)))") ; set -- $IDS ; curl -s -b /tmp/kb.cookies -X DELETE "http://localhost:5110/api/boards/$B/statuses/$1" -o /dev/null -w "d1=%{http_code}\n" ; curl -s -b /tmp/kb.cookies -X DELETE "http://localhost:5110/api/boards/$B/statuses/$2" -o /dev/null -w "d2=%{http_code}\n" ; curl -s -b /tmp/kb.cookies -X DELETE "http://localhost:5110/api/boards/$B/statuses/$3" -w "\nd3=%{http_code}\n"
```

Ожидаемо: `d1=204`, `d2=204`, `d3=409` с телом `Нельзя удалить последнюю колонку`.

- [ ] Коммит:

```bash
cd . && git add Models/Requests/StatusRequest.cs Services/StatusService.cs Controllers/StatusController.cs && git commit -m "Бек: CRUD колонок доски с 409 на непустую и последнюю колонку"
```

---

## Task 16: `PATCH /api/boards/{boardId}/statuses/{statusId}/position`

**Files:**
- Create: `./Models/Requests/StatusPositionRequest.cs`
- Create: `./Models/Responses/StatusPositionResponse.cs`
- Modify: `./Services/StatusService.cs` (новый метод `MoveStatusAsync`)
- Modify: `./Controllers/StatusController.cs` (новый экшен)

- [ ] Создать `Models/Requests/StatusPositionRequest.cs`:

```csharp
namespace KanbanBoard.Models.Requests
{
    public class StatusPositionRequest
    {
        public int Position { get; set; }
    }
}
```

- [ ] Создать `Models/Responses/StatusPositionResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class StatusPositionResponse
    {
        public int Id { get; set; }
        public int Order { get; set; }
    }
}
```

- [ ] В `Services/StatusService.cs` добавить метод перестановки перед `CreateStatusAsync`. Заменить:

```csharp
        public async Task<StatusResponse?> CreateStatusAsync(int boardId, int userId, StatusRequest request, CancellationToken ct)
```

на:

```csharp
        public async Task<List<StatusPositionResponse>?> MoveStatusAsync(int boardId, int userId, int statusId, StatusPositionRequest request, CancellationToken ct)
        {
            if (!await IsUserBoardOwnerAsync(boardId, userId, ct))
                return null;

            var statuses = await _db.Statuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.StatusId)
                .ToListAsync(ct);

            var movedStatus = statuses.FirstOrDefault(s => s.StatusId == statusId);
            if (movedStatus == null)
                return null;

            statuses.Remove(movedStatus);

            var position = request.Position;
            if (position < 0)
                position = 0;
            if (position > statuses.Count)
                position = statuses.Count;

            statuses.Insert(position, movedStatus);

            for (var i = 0; i < statuses.Count; i++)
                statuses[i].Order = i;

            await _db.SaveChangesAsync(ct);

            return statuses
                .Select(s => new StatusPositionResponse
                {
                    Id = s.StatusId,
                    Order = s.Order
                })
                .ToList();
        }

        public async Task<StatusResponse?> CreateStatusAsync(int boardId, int userId, StatusRequest request, CancellationToken ct)
```

- [ ] В `Controllers/StatusController.cs` добавить экшен перед `DeleteBoardStatus`. Заменить:

```csharp
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("api/boards/{boardId}/statuses/{statusId}")]
        public async Task<IActionResult> DeleteBoardStatus(int boardId, int statusId, CancellationToken ct)
```

на:

```csharp
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/statuses/{statusId}/position")]
        public async Task<IActionResult> MoveBoardStatus(int boardId, int statusId, [FromBody] StatusPositionRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _statusService.MoveStatusAsync(boardId, userId, statusId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Route("api/boards/{boardId}/statuses/{statusId}")]
        public async Task<IActionResult> DeleteBoardStatus(int boardId, int statusId, CancellationToken ct)
```

- [ ] Проверка: перезапустить приложение, перетащить последнюю колонку в начало, затем задать позицию за пределами диапазона (кламп):

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Перестановка колонок","description":"p"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; DONE=$(curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print(json.load(sys.stdin)[2]['statusId'])") ; echo "board=$B done=$DONE" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/statuses/$DONE/position" -H "Content-Type: application/json" -d '{"position":0}' -w "\nmove=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print('after_move=',[(s['statusName'],s['order']) for s in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/statuses/$DONE/position" -H "Content-Type: application/json" -d '{"position":99}' -o /dev/null -w "clamp_high=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print('after_clamp_high=',[(s['statusName'],s['order']) for s in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/statuses/$DONE/position" -H "Content-Type: application/json" -d '{"position":-5}' -o /dev/null -w "clamp_low=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;print('after_clamp_low=',[(s['statusName'],s['order']) for s in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/statuses/99999999/position" -H "Content-Type: application/json" -d '{"position":0}' -o /dev/null -w "missing=%{http_code}\n"
```

Ожидаемо: `move=200` и в теле массив из трёх объектов `{"id":…,"order":…}`; `after_move= [('Done', 0), ('To Do', 1), ('In Progress', 2)]`; `clamp_high=200` и `after_clamp_high= [('To Do', 0), ('In Progress', 1), ('Done', 2)]`; `clamp_low=200` и `after_clamp_low= [('Done', 0), ('To Do', 1), ('In Progress', 2)]`; `missing=404`.

- [ ] Коммит:

```bash
cd . && git add Models/Requests/StatusPositionRequest.cs Models/Responses/StatusPositionResponse.cs Services/StatusService.cs Controllers/StatusController.cs && git commit -m "Бек: перестановка колонок доски с клампом позиции и переиндексацией"
```

---

## Task 17: `PATCH /api/boards/{boardId}/tasks/{taskId}/position`

**Files:**
- Create: `./Models/Requests/TaskPositionRequest.cs`
- Create: `./Models/Responses/TaskPositionResponse.cs`
- Modify: `./Services/TaskService.cs` (новый метод `MoveTaskAsync`)
- Modify: `./Controllers/TaskController.cs` (новый экшен)

- [ ] Создать `Models/Requests/TaskPositionRequest.cs`:

```csharp
namespace KanbanBoard.Models.Requests
{
    public class TaskPositionRequest
    {
        public int StatusId { get; set; }
        public int Position { get; set; }
    }
}
```

- [ ] Создать `Models/Responses/TaskPositionResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class TaskPositionResponse
    {
        public int Id { get; set; }
        public int StatusId { get; set; }
        public int Order { get; set; }
    }
}
```

- [ ] В `Services/TaskService.cs` добавить метод переноса перед `ChangeTaskStatusAsync`. Заменить:

```csharp
        public async Task<StatusHistoryResponse?> ChangeTaskStatusAsync(int boardId, int userId, int taskId, StatusHistoryRequest request ,CancellationToken ct)
```

на:

```csharp
        public async Task<List<TaskPositionResponse>?> MoveTaskAsync(int boardId, int userId, int taskId, TaskPositionRequest request, CancellationToken ct)
        {
            var currentBoardUser = await _db.BoardUsers
                .FirstOrDefaultAsync(bu => bu.BoardId == boardId && bu.UserId == userId && !bu.IsDeleted, ct);
            if (currentBoardUser == null)
                return null;

            var movedTask = await _db.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.BoardId == boardId, ct);
            if (movedTask == null)
                return null;

            var targetStatus = await _db.Statuses
                .FirstOrDefaultAsync(s => s.StatusId == request.StatusId && s.BoardId == boardId, ct);
            if (targetStatus == null)
                return null;

            var sourceStatusId = movedTask.StatusId;
            var isStatusChanged = sourceStatusId != targetStatus.StatusId;

            var targetTasks = await _db.Tasks
                .Where(t => t.BoardId == boardId && t.StatusId == targetStatus.StatusId && t.TaskId != taskId)
                .OrderBy(t => t.Order)
                .ThenBy(t => t.TaskId)
                .ToListAsync(ct);

            var sourceTasks = new List<Task>();

            if (isStatusChanged)
            {
                sourceTasks = await _db.Tasks
                    .Where(t => t.BoardId == boardId && t.StatusId == sourceStatusId && t.TaskId != taskId)
                    .OrderBy(t => t.Order)
                    .ThenBy(t => t.TaskId)
                    .ToListAsync(ct);
            }

            var position = request.Position;
            if (position < 0)
                position = 0;
            if (position > targetTasks.Count)
                position = targetTasks.Count;

            movedTask.StatusId = targetStatus.StatusId;
            targetTasks.Insert(position, movedTask);

            for (var i = 0; i < targetTasks.Count; i++)
                targetTasks[i].Order = i;

            for (var i = 0; i < sourceTasks.Count; i++)
                sourceTasks[i].Order = i;

            if (isStatusChanged)
            {
                var history = new TaskStatusHistory
                {
                    TaskId = movedTask.TaskId,
                    StatusId = targetStatus.StatusId,
                    AuthorId = currentBoardUser.BoardUserId,
                    ChangeDate = DateTime.UtcNow
                };

                _db.TaskStatusHistories.Add(history);
            }

            await _db.SaveChangesAsync(ct);

            var affectedTasks = new List<Task>(targetTasks);
            affectedTasks.AddRange(sourceTasks);

            return affectedTasks
                .Select(t => new TaskPositionResponse
                {
                    Id = t.TaskId,
                    StatusId = t.StatusId,
                    Order = t.Order
                })
                .ToList();
        }

        public async Task<StatusHistoryResponse?> ChangeTaskStatusAsync(int boardId, int userId, int taskId, StatusHistoryRequest request ,CancellationToken ct)
```

- [ ] В `Controllers/TaskController.cs` добавить экшен перед `ChangeStatus`. Заменить:

```csharp
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/status")]
        public async Task<IActionResult> ChangeStatus(int boardId, int taskId, [FromBody] StatusHistoryRequest request, CancellationToken ct)
```

на:

```csharp
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/position")]
        public async Task<IActionResult> MoveTask(int boardId, int taskId, [FromBody] TaskPositionRequest request, CancellationToken ct)
        {
            var userId = GetUserId();

            var result = await _taskService.MoveTaskAsync(boardId, userId, taskId, request, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Route("api/boards/{boardId}/tasks/{taskId}/status")]
        public async Task<IActionResult> ChangeStatus(int boardId, int taskId, [FromBody] StatusHistoryRequest request, CancellationToken ct)
```

- [ ] Проверка: перезапустить приложение и прогнать сценарий из §9 спеки — перестановка внутри колонки, перенос между колонками, кламп, чужая доска:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"DnD","description":"d"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; S=$(curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/statuses" | python3 -c "import sys,json;d=json.load(sys.stdin);print(d[0]['statusId'],d[1]['statusId'])") ; set -- $S ; S1=$1 ; S2=$2 ; for n in A B C; do curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d "{\"taskName\":\"$n\",\"deadline\":\"2026-12-31T12:00:00Z\",\"workerId\":null}" -o /dev/null ; done ; TASKS=$(curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks" | python3 -c "import sys,json;print(' '.join(str(t['taskId']) for t in json.load(sys.stdin)))") ; set -- $TASKS ; TA=$1 ; TB=$2 ; TC=$3 ; echo "board=$B s1=$S1 s2=$S2 A=$TA B=$TB C=$TC" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/tasks/$TC/position" -H "Content-Type: application/json" -d "{\"statusId\":$S1,\"position\":0}" -w "\nreorder=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks" | python3 -c "import sys,json;print('after_reorder=',[(t['taskName'],t['order']) for t in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/tasks/$TC/position" -H "Content-Type: application/json" -d "{\"statusId\":$S2,\"position\":0}" -w "\nmove_between=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks" | python3 -c "import sys,json;print('after_move=',[(t['taskName'],t['status']['statusName'],t['order']) for t in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/tasks/$TA/position" -H "Content-Type: application/json" -d "{\"statusId\":$S1,\"position\":99}" -o /dev/null -w "clamp=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks" | python3 -c "import sys,json;print('after_clamp=',[(t['taskName'],t['status']['statusName'],t['order']) for t in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$TC/history" | python3 -c "import sys,json;print('history=',[h['status']['statusName'] for h in json.load(sys.stdin)])" ; curl -s -b /tmp/kb.cookies -X PATCH "http://localhost:5110/api/boards/$B/tasks/$TC/position" -H "Content-Type: application/json" -d '{"statusId":99999999,"position":0}' -o /dev/null -w "alien_status=%{http_code}\n"
```

Ожидаемо:
- `reorder=200`, в теле массив из трёх `{"id":…,"statusId":…,"order":…}`; `after_reorder= [('C', 0), ('A', 1), ('B', 2)]`;
- `move_between=200`; `after_move=` C в `In Progress` c `order 0`, A и B в `To Do` c `order 0` и `1`;
- `clamp=200`; `after_clamp=` A в `To Do` с наибольшим `order` (=1), B — `order 0`;
- `history= ['In Progress', 'To Do']` (перенос между колонками записал строку истории);
- `alien_status=404`.

- [ ] Коммит:

```bash
cd . && git add Models/Requests/TaskPositionRequest.cs Models/Responses/TaskPositionResponse.cs Services/TaskService.cs Controllers/TaskController.cs && git commit -m "Бек: перенос задачи между колонками и внутри колонки с переиндексацией и историей"
```

---

## Task 18: `CommentResponse` — `TaskId` и вложения

**Files:**
- Modify: `./Models/Responses/CommentResponse.cs`
- Modify: `./Services/CommentService.cs`

- [ ] Перезаписать `Models/Responses/CommentResponse.cs`:

```csharp
namespace KanbanBoard.Models.Responses
{
    public class CommentResponse
    {
        public int CommentId { get; set; }
        public int TaskId { get; set; }
        public string? Text { get; set; }
        public DateTime MadeDate { get; set; }
        public bool IsEdited { get; set; }
        public UserResponse Author { get; set; } = null!;
        public List<AttachmentResponse> Attachments { get; set; } = new List<AttachmentResponse>();
    }
}
```

- [ ] В `Services/CommentService.cs` заменить шапку файла и начало класса (добавляется общая проекция комментария вместе с вложениями). Заменить:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Conventions;
using System.Threading.Tasks;

namespace KanbanBoard.Services
{
    public class CommentService
    {
        private readonly KanbanBoardDbContext _db;

        public CommentService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }
```

на:

```csharp
using KanbanBoard.Data;
using KanbanBoard.Models;
using KanbanBoard.Models.Requests;
using KanbanBoard.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Conventions;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace KanbanBoard.Services
{
    public class CommentService
    {
        private readonly KanbanBoardDbContext _db;

        public CommentService(KanbanBoardDbContext dbContext)
        {
            _db = dbContext;
        }

        private static readonly Expression<Func<Comment, CommentResponse>> ToCommentResponse = c => new CommentResponse
        {
            CommentId = c.CommentId,
            TaskId = c.TaskId,
            Text = c.Text,
            MadeDate = c.DateOfMade,
            IsEdited = c.IsEdited,
            Author = new UserResponse
            {
                UserId = c.Author.UserId,
                Login = c.Author.User.Login
            },
            Attachments = c.Attachments.Select(a => new AttachmentResponse
            {
                AttachmentId = a.AttachmentId,
                FileName = a.FileName,
                DateOfUpload = a.DateOfUpload,
                Uploader = new UserResponse
                {
                    UserId = a.Uploader.UserId,
                    Login = a.Uploader.User.Login
                }
            }).ToList()
        };
```

- [ ] В `Services/CommentService.cs` заменить хвост метода `EditTaskCommentAsync`:

```csharp
            commentToUpdate.Text = request.Text;
            commentToUpdate.IsEdited = true;

            await _db.SaveChangesAsync(ct);

            var updatedComment = await _db.Comments
                .Include(c => c.Author).ThenInclude(bu => bu.User)
                .FirstAsync(c => c.CommentId == commentToUpdate.CommentId, ct);

            return new CommentResponse
            {
                CommentId = updatedComment.CommentId,
                Text = updatedComment.Text,
                MadeDate = updatedComment.DateOfMade,
                Author = new UserResponse
                {
                    UserId = updatedComment.Author.UserId,
                    Login = updatedComment.Author.User.Login
                },
                IsEdited = updatedComment.IsEdited,
            };
        }
```

на:

```csharp
            commentToUpdate.Text = request.Text;
            commentToUpdate.IsEdited = true;

            await _db.SaveChangesAsync(ct);

            return await _db.Comments
                .Where(c => c.CommentId == commentToUpdate.CommentId)
                .Select(ToCommentResponse)
                .FirstAsync(ct);
        }
```

- [ ] В `Services/CommentService.cs` заменить хвост метода `CreateCommentToTaskAsync`:

```csharp
            _db.Comments.Add(newComment);

            await _db.SaveChangesAsync(ct);

            var createdComment = await _db.Comments
                .Include(c => c.Author).ThenInclude(bu => bu.User)
                .FirstAsync(c => c.CommentId == newComment.CommentId, ct);

            return new CommentResponse
            {
                CommentId = newComment.CommentId,
                Text = newComment.Text,
                MadeDate = newComment.DateOfMade,
                Author = new UserResponse
                {
                    UserId = createdComment.Author.UserId,
                    Login = createdComment.Author.User.Login
                },
                IsEdited = createdComment.IsEdited,
            };

        }
```

на:

```csharp
            _db.Comments.Add(newComment);

            await _db.SaveChangesAsync(ct);

            return await _db.Comments
                .Where(c => c.CommentId == newComment.CommentId)
                .Select(ToCommentResponse)
                .FirstAsync(ct);

        }
```

- [ ] В `Services/CommentService.cs` заменить хвост метода `GetAllCommentsOfTaskAsync`:

```csharp
            var comments = await _db.Comments
                .Where(c => c.TaskId == taskId)
                .Select(c => new CommentResponse
                {
                    Author = new UserResponse { Login = c.Author.User.Login, UserId = c.Author.UserId },
                    CommentId = c.CommentId,
                    MadeDate = c.DateOfMade,
                    Text = c.Text,
                    IsEdited = c.IsEdited,

                })
                .ToListAsync(ct);

            return comments;
```

на:

```csharp
            var comments = await _db.Comments
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.DateOfMade)
                .ThenBy(c => c.CommentId)
                .Select(ToCommentResponse)
                .ToListAsync(ct);

            return comments;
```

- [ ] Проверка: перезапустить приложение, создать комментарий, приложить к нему файл и прочитать список комментариев:

```bash
pkill -f "KanbanBoard.dll" ; pkill -f "dotnet run" ; sleep 2 ; cd . && dotnet build 2>&1 | tail -3 && (nohup dotnet run --no-build --launch-profile http > /tmp/kb-run.log 2>&1 &) && sleep 12 && rm -f /tmp/kb.cookies && echo "вложение" > /tmp/kb-file.txt && curl -s -c /tmp/kb.cookies -X POST http://localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"login":"kbtest","password":"secret123"}' -o /dev/null -w "login=%{http_code}\n" ; B=$(curl -s -b /tmp/kb.cookies -X POST http://localhost:5110/api/boards -H "Content-Type: application/json" -d '{"name":"Комментарии","description":"c"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['boardId'])") ; T=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks" -H "Content-Type: application/json" -d '{"taskName":"С комментами","deadline":"2026-12-31T12:00:00Z","workerId":null}' | python3 -c "import sys,json;print(json.load(sys.stdin)['taskId'])") ; C=$(curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/tasks/$T/comments" -H "Content-Type: application/json" -d '{"text":"Первый коммент"}' | python3 -c "import sys,json;d=json.load(sys.stdin);print(d['commentId'],'created taskId=',d['taskId'],'attachments=',d['attachments'],file=sys.stderr);print(d['commentId'])") ; curl -s -b /tmp/kb.cookies -X POST "http://localhost:5110/api/boards/$B/comments/$C/attachments" -F "file=@/tmp/kb-file.txt" -o /dev/null -w "upload=%{http_code}\n" ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$T/comments" ; echo ; curl -s -b /tmp/kb.cookies "http://localhost:5110/api/boards/$B/tasks/$T" | python3 -c "import sys,json;d=json.load(sys.stdin);print('task counts: comments=',d['commentsCount'],'attachments=',d['attachmentsCount'])"
```

Ожидаемо: в stderr `created taskId= <T> attachments= []`; `upload=200`; список комментариев — один объект с `commentId`, `taskId`, `text`, `madeDate`, `isEdited`, `author`, и `attachments` из одного элемента (`fileName` = `kb-file.txt`, без `filePath`); `task counts: comments= 1 attachments= 0` (вложение висит на комментарии, а не на задаче — как требует §6.4).

- [ ] Коммит:

```bash
cd . && git add Models/Responses/CommentResponse.cs Services/CommentService.cs && git commit -m "Бек: CommentResponse отдаёт задачу и вложения комментария"
```

---

## Приёмка фаз 1–2

- [ ] Финальная сборка без ошибок:

```bash
cd . && dotnet build 2>&1 | tail -5
```

Ожидаемо: `Build succeeded.`, `0 Error(s)`.

- [ ] Прогон по Swagger (§9 спеки): перезапустить приложение, открыть `http://localhost:5110/swagger`, залогиниться через `POST /api/auth/login` прямо в Swagger (его `custom.js` отправляет куки) и вручную проверить каждый новый/изменённый эндпоинт на успех и на 401/404/400/409:
  - `POST /api/auth/login` с неверным паролем → 401;
  - `GET /api/auth/me` без куки → 401;
  - `POST /api/boards/{boardId}/statuses` с именем длиннее 100 символов → 400 с `errors.Name`;
  - `DELETE` непустой колонки → 409, последней колонки → 409;
  - `PATCH .../statuses/{id}/position` и `PATCH .../tasks/{id}/position` → 200 со списком затронутых;
  - `GET .../tasks/{taskId}` с чужим `boardId` → 404;
  - `GET .../tasks/{taskId}/history` → список от новых к старым.

- [ ] Показать результат Павлу: фазы 1–2 приняты — можно начинать фазу 3 (фронт-каркас) по отдельному плану.
