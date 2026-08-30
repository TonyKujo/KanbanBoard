using KanbanBoard.Data;
using KanbanBoard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
});

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

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Браузер обязан ревалидировать js/css при каждой загрузке —
        // иначе правки фронта не видны по F5 из-за эвристического кэша.
        ctx.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
