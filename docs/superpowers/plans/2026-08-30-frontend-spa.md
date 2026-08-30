# KanbanBoard Frontend SPA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox ("- [ ]") syntax for tracking.

**Goal:** Реализовать фазы 3–5 из §10 спеки `docs/superpowers/specs/2026-08-30-kanban-frontend-design.md`: SPA-фронтенд на Vue 3 без сборки поверх существующего ASP.NET Core бека — каркас (оболочка, темы, логин, сетка досок, сайдбар), доска (колонки, карточки, DnD, поиск) и панель задачи + настройки доски.

**Architecture:** Единая точка входа — Razor `Views/Home/Index.cshtml` (отдаётся `HomeController.Index` на `/`), которая подключает css/js и содержит `<div id="app">`. Весь фронт лежит в `wwwroot`, отдаётся тем же приложением (same-origin, CORS не нужен). Роутинг — собственный hash-роутер (`#/login`, `#/`, `#/boards/5`, `#/boards/5/tasks/42`), состояние — один `reactive`-стор, общение с беком — только через `api.js`. Живых обновлений нет (осознанное ограничение v1, §8 спеки).

**Tech Stack:** Vue 3.5.13 (ES-модуль, вендорится в репозиторий, без node/npm/бандлера), нативные ES-модули, Composition API через template-строки, нативный HTML5 drag&drop, CSS custom properties для двух тем + system-режим. Бек — ASP.NET Core MVC .NET 9 + PostgreSQL, cookie-auth (уже есть).

**Предпосылки:** фазы 1–2 (бек) из §10 спеки реализованы и приняты по Swagger. Формы DTO — ровно §6.4 спеки. Задачи 1–12 (фаза 3) дополнительно проверяемы и на текущем беке: `/api/auth/*` и `/api/boards*` существуют уже сейчас.

---

## Структура файлов

**Создаются:**

| Файл | Ответственность |
|---|---|
| `wwwroot/lib/vue.esm-browser.prod.js` | Вендоренный Vue 3.5.13 (скачивается curl'ом, не пишется руками) |
| `wwwroot/css/tokens.css` | Дизайн-токены обеих тем + system-режим через `@media` |
| `wwwroot/css/app.css` | Все стили приложения (одним файлом, чтобы не размазывать правки по фазам) |
| `wwwroot/js/app.js` | `createApp`, hash-роутер (`parseHash` + слушатель `hashchange`), корневой компонент, бутстрап сессии |
| `wwwroot/js/api.js` | Единственное место общения с беком: fetch-обёртка, нормализация ошибок, UTC-даты, клиентские проверки длин |
| `wwwroot/js/store.js` | Реактивное общее состояние (юзер, доски, доска, задачи, тосты, route) и операции над ним |
| `wwwroot/js/strings.js` | Все русские строки UI: подписи, ошибки, пустые состояния |
| `wwwroot/js/ui.js` | Общие хелперы отображения: даты, дедлайн-бейдж, инициалы и цвет аватара |
| `wwwroot/js/components/Toast.js` | Стек тостов |
| `wwwroot/js/components/ThemeToggle.js` | Переключатель темы (Системная / Светлая / Тёмная), `localStorage` `kb.theme` |
| `wwwroot/js/components/LoginPage.js` | Вход / регистрация |
| `wwwroot/js/components/Sidebar.js` | Левый сайдбар: список досок, тема, выход |
| `wwwroot/js/components/BoardsGrid.js` | Сетка карточек досок + создание доски |
| `wwwroot/js/components/TaskCard.js` | Карточка задачи: дедлайн-бейдж, аватар, счётчики, источник DnD |
| `wwwroot/js/components/TaskColumn.js` | Колонка: заголовок, список карточек, drop-зона, «+ Задача» |
| `wwwroot/js/components/BoardView.js` | Экран доски: шапка, поиск, колонки, модалка создания задачи, панель задачи, модалка настроек |
| `wwwroot/js/components/TaskPanel.js` | Широкая панель задачи в две колонки: поля, статус, вложения, история |
| `wwwroot/js/components/CommentList.js` | Комментарии задачи: список, добавление (оптимистично / со спиннером при файле) |
| `wwwroot/js/components/BoardSettingsModal.js` | Настройки доски: Участники / Колонки / О доске |

**Изменяются:**

| Файл | Что меняется |
|---|---|
| `Program.cs` (строка ~53) | `app.UseStaticFiles()` перед `app.UseRouting()` — гарантия отдачи правки js/css без пересборки (§3 спеки; если фаза 1 бека это уже добавила — шаг пропускается) |
| `Views/Shared/_Layout.cshtml` (весь файл, 14 строк) | Тонкая оболочка: `lang="ru"`, инлайн no-flash скрипт темы, подключение `tokens.css`/`app.css`, без `<main>` |
| `Views/Home/Index.cshtml` (весь файл, 5 строк) | `<div id="app">` + `@section Scripts` с `<script type="module" src="/js/app.js">` |

**Отклонение от §3 спеки (осознанное):** добавлен `wwwroot/js/ui.js`. В §3 его нет, но хелперы форматирования дат, дедлайна и аватаров нужны в шести компонентах — без общего модуля это шесть копий одного кода (нарушение DRY). Состояния в нём нет, только чистые функции.

---

### Task 1: Вендоринг Vue 3.5.13 и отдача статики

**Files:**
- Create: `./wwwroot/lib/vue.esm-browser.prod.js` (скачивается)
- Modify: `./Program.cs` (строка 53, перед `app.UseRouting();`)

- [ ] Создать каталог для вендоренной библиотеки:
```bash
mkdir -p ./wwwroot/lib
```

- [ ] Скачать Vue 3.5.13 (версия пиним точно, §3 спеки):
```bash
curl -fsSL https://unpkg.com/vue@3.5.13/dist/vue.esm-browser.prod.js \
  -o ./wwwroot/lib/vue.esm-browser.prod.js
```

- [ ] Проверить, что скачался именно нужный файл и именно нужной версии:
```bash
grep -c "vue v3.5.13" ./wwwroot/lib/vue.esm-browser.prod.js
```
Ожидаемый вывод: `1`

- [ ] Проверить, что файл целый (не HTML-заглушка и не обрезан):
```bash
wc -c < ./wwwroot/lib/vue.esm-browser.prod.js
```
Ожидаемый вывод: число больше `100000` (реально ~157000 байт).

- [ ] Проверить, что модуль экспортирует нужное:
```bash
grep -c "export{" ./wwwroot/lib/vue.esm-browser.prod.js
```
Ожидаемый вывод: `1`

- [ ] Открыть `Program.cs` и проверить, есть ли уже `app.UseStaticFiles();`:
```bash
grep -n "UseStaticFiles" ./Program.cs
```
Если строка найдена (фаза 1 бека уже отработала) — следующий шаг пропустить.

- [ ] В `Program.cs` добавить отдачу статики. Найти фрагмент:
```csharp
app.UseRouting();

app.UseAuthentication();
```
Заменить на:
```csharp
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
```

- [ ] Собрать проект:
```bash
cd . && dotnet build
```
Ожидаемый вывод: `Build succeeded.` и `0 Error(s)`.

- [ ] Запустить приложение и проверить, что Vue отдаётся статикой:
```bash
cd . && dotnet run
```
В другом терминале:
```bash
curl -s -o /dev/null -w "%{http_code} %{content_type} %{size_download}\n" http://localhost:5110/lib/vue.esm-browser.prod.js
```
Ожидаемый вывод: `200 text/javascript` (или `application/javascript`) и размер больше `100000`. Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/lib/vue.esm-browser.prod.js Program.cs && git commit -m "Вендоринг Vue 3.5.13 и включение отдачи статики"
```

---

### Task 2: Дизайн-токены обеих тем

**Files:**
- Create: `./wwwroot/css/tokens.css`

- [ ] Создать каталог стилей:
```bash
mkdir -p ./wwwroot/css
```

- [ ] Создать `wwwroot/css/tokens.css` со следующим содержимым (светлая тема — на `:root`; тёмная — дважды: явным атрибутом и через `prefers-color-scheme` для system-режима, когда атрибут не выставлен):
```css
/* Дизайн-токены KanbanBoard.
   Светлая тема — «строгая»: белый фон, серо-синяя палитра, синий акцент, малые скругления.
   Тёмная — «Linear»: графит, индиго-акцент, тонкие рамки вместо теней.
   Три состояния темы: light / dark / system. При system атрибут data-theme не ставится,
   поэтому тёмные значения описаны и в @media, и в [data-theme="dark"]. */

:root {
    --kb-bg: #f4f5f7;
    --kb-surface: #ffffff;
    --kb-surface-2: #eceef2;
    --kb-surface-3: #e2e5ea;
    --kb-border: #d6dae1;
    --kb-border-strong: #b9c0ca;
    --kb-text: #1b2333;
    --kb-text-muted: #616c85;
    --kb-text-faint: #8b95a8;
    --kb-accent: #2563eb;
    --kb-accent-hover: #1d4ed8;
    --kb-accent-soft: #e5edff;
    --kb-on-accent: #ffffff;
    --kb-danger: #d32f2f;
    --kb-danger-hover: #b02525;
    --kb-danger-soft: #fdeaea;
    --kb-warning: #b46a00;
    --kb-warning-soft: #fdf1de;
    --kb-success: #17803d;

    --kb-radius: 4px;
    --kb-radius-lg: 8px;
    --kb-shadow: 0 1px 2px rgba(16, 24, 40, .08), 0 2px 8px rgba(16, 24, 40, .06);
    --kb-shadow-lg: 0 12px 40px rgba(16, 24, 40, .20);
    --kb-overlay: rgba(20, 26, 38, .42);

    --kb-sidebar-w: 248px;
    --kb-column-w: 288px;
    --kb-panel-w: 58vw;

    --kb-font: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    --kb-font-mono: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
}

@media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
        --kb-bg: #16181d;
        --kb-surface: #1c1f26;
        --kb-surface-2: #23262f;
        --kb-surface-3: #2b2f3a;
        --kb-border: #30343f;
        --kb-border-strong: #434958;
        --kb-text: #e5e7ee;
        --kb-text-muted: #98a0b3;
        --kb-text-faint: #757e93;
        --kb-accent: #6366f1;
        --kb-accent-hover: #7c7ff5;
        --kb-accent-soft: #23244a;
        --kb-on-accent: #ffffff;
        --kb-danger: #ef5350;
        --kb-danger-hover: #e03c39;
        --kb-danger-soft: #3a1f1f;
        --kb-warning: #e0a34a;
        --kb-warning-soft: #382c19;
        --kb-success: #4ade80;

        --kb-shadow: none;
        --kb-shadow-lg: 0 16px 48px rgba(0, 0, 0, .55);
        --kb-overlay: rgba(8, 10, 14, .62);
    }
}

:root[data-theme="dark"] {
    --kb-bg: #16181d;
    --kb-surface: #1c1f26;
    --kb-surface-2: #23262f;
    --kb-surface-3: #2b2f3a;
    --kb-border: #30343f;
    --kb-border-strong: #434958;
    --kb-text: #e5e7ee;
    --kb-text-muted: #98a0b3;
    --kb-text-faint: #757e93;
    --kb-accent: #6366f1;
    --kb-accent-hover: #7c7ff5;
    --kb-accent-soft: #23244a;
    --kb-on-accent: #ffffff;
    --kb-danger: #ef5350;
    --kb-danger-hover: #e03c39;
    --kb-danger-soft: #3a1f1f;
    --kb-warning: #e0a34a;
    --kb-warning-soft: #382c19;
    --kb-success: #4ade80;

    --kb-shadow: none;
    --kb-shadow-lg: 0 16px 48px rgba(0, 0, 0, .55);
    --kb-overlay: rgba(8, 10, 14, .62);
}
```

- [ ] Проверить, что оба тёмных блока определяют одинаковый набор токенов (защита от расхождения при правках):
```bash
cd . && grep -c -- "--kb-bg: #16181d" wwwroot/css/tokens.css
```
Ожидаемый вывод: `2`

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/css/tokens.css && git commit -m "Дизайн-токены светлой и тёмной темы"
```

---

### Task 3: Общие стили приложения

**Files:**
- Create: `./wwwroot/css/app.css`

Стили пишутся сразу на все три фазы одним файлом — чтобы не редактировать CSS в каждой последующей задаче.

- [ ] Создать `wwwroot/css/app.css` со следующим содержимым:
```css
/* Общие стили KanbanBoard. Все цвета — только через токены из tokens.css. */

*, *::before, *::after { box-sizing: border-box; }

html, body { height: 100%; }

body {
    margin: 0;
    font-family: var(--kb-font);
    font-size: 14px;
    line-height: 1.45;
    color: var(--kb-text);
    background: var(--kb-bg);
    -webkit-font-smoothing: antialiased;
}

button, input, textarea, select { font: inherit; color: inherit; }

/* ---------- Каркас ---------- */

.kb-boot {
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100vh;
    color: var(--kb-text-muted);
}

.kb-shell { display: flex; height: 100vh; overflow: hidden; }

.kb-main { flex: 1; min-width: 0; overflow: auto; }

/* ---------- Сайдбар ---------- */

.kb-sidebar {
    width: var(--kb-sidebar-w);
    flex: 0 0 var(--kb-sidebar-w);
    display: flex;
    flex-direction: column;
    background: var(--kb-surface);
    border-right: 1px solid var(--kb-border);
}

.kb-sidebar__brand {
    padding: 16px 16px 12px;
    font-size: 15px;
    font-weight: 700;
    letter-spacing: .2px;
}

.kb-sidebar__section {
    padding: 8px 16px 4px;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: .6px;
    color: var(--kb-text-faint);
}

.kb-sidebar__list { flex: 1; overflow: auto; padding: 0 8px 8px; }

.kb-sidebar__item {
    display: block;
    padding: 7px 10px;
    margin-bottom: 2px;
    border-radius: var(--kb-radius);
    color: var(--kb-text);
    text-decoration: none;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.kb-sidebar__item:hover { background: var(--kb-surface-2); }

.kb-sidebar__item.is-active { background: var(--kb-accent-soft); color: var(--kb-accent); font-weight: 600; }

.kb-sidebar__empty { padding: 8px 10px; color: var(--kb-text-faint); font-size: 13px; }

.kb-sidebar__footer { border-top: 1px solid var(--kb-border); padding: 12px; }

.kb-sidebar__user {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 12px;
    font-size: 13px;
    color: var(--kb-text-muted);
}

.kb-sidebar__user span { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* ---------- Кнопки и поля ---------- */

.kb-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 7px 14px;
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius);
    background: var(--kb-surface);
    color: var(--kb-text);
    cursor: pointer;
    white-space: nowrap;
}

.kb-btn:hover { background: var(--kb-surface-2); }
.kb-btn:disabled { opacity: .55; cursor: default; }
.kb-btn--primary { background: var(--kb-accent); border-color: var(--kb-accent); color: var(--kb-on-accent); }
.kb-btn--primary:hover { background: var(--kb-accent-hover); }
.kb-btn--danger { background: var(--kb-danger); border-color: var(--kb-danger); color: #fff; }
.kb-btn--danger:hover { background: var(--kb-danger-hover); }
.kb-btn--ghost { background: transparent; border-color: transparent; color: var(--kb-text-muted); }
.kb-btn--ghost:hover { background: var(--kb-surface-2); color: var(--kb-text); }
.kb-btn--sm { padding: 4px 9px; font-size: 13px; }

.kb-field { margin-bottom: 14px; }
.kb-field__label { display: block; margin-bottom: 5px; font-size: 12px; color: var(--kb-text-muted); }

.kb-input, .kb-textarea, .kb-select {
    width: 100%;
    padding: 7px 10px;
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius);
    background: var(--kb-surface);
    outline: none;
}

.kb-input:focus, .kb-textarea:focus, .kb-select:focus { border-color: var(--kb-accent); }
.kb-textarea { resize: vertical; min-height: 72px; }
.kb-field__error { margin-top: 5px; font-size: 12px; color: var(--kb-danger); }

.kb-form-error {
    margin: 10px 0 0;
    padding: 8px 10px;
    border-radius: var(--kb-radius);
    background: var(--kb-danger-soft);
    color: var(--kb-danger);
    font-size: 13px;
}

/* ---------- Логин ---------- */

.kb-auth {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    padding: 24px;
}

.kb-auth__card {
    width: 100%;
    max-width: 380px;
    padding: 26px;
    background: var(--kb-surface);
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius-lg);
    box-shadow: var(--kb-shadow);
}

.kb-auth__title { margin: 0 0 4px; font-size: 20px; }
.kb-auth__subtitle { margin: 0 0 18px; color: var(--kb-text-muted); font-size: 13px; }

.kb-tabs { display: flex; gap: 4px; margin-bottom: 18px; padding: 3px; background: var(--kb-surface-2); border-radius: var(--kb-radius); }

.kb-tabs__item {
    flex: 1;
    padding: 6px 10px;
    border: none;
    border-radius: var(--kb-radius);
    background: transparent;
    color: var(--kb-text-muted);
    cursor: pointer;
}

.kb-tabs__item.is-active { background: var(--kb-surface); color: var(--kb-text); font-weight: 600; box-shadow: var(--kb-shadow); }

/* ---------- Сетка досок ---------- */

.kb-page { padding: 24px 28px; }
.kb-page__head { display: flex; align-items: center; gap: 12px; margin-bottom: 18px; }
.kb-page__title { margin: 0; font-size: 20px; flex: 1; }

.kb-boards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 14px; }

.kb-board-card {
    display: block;
    padding: 16px;
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius-lg);
    background: var(--kb-surface);
    color: inherit;
    text-decoration: none;
    box-shadow: var(--kb-shadow);
}

.kb-board-card:hover { border-color: var(--kb-accent); }
.kb-board-card__name { font-weight: 600; margin-bottom: 6px; }
.kb-board-card__desc { color: var(--kb-text-muted); font-size: 13px; min-height: 36px; }
.kb-board-card__meta { display: flex; align-items: center; gap: 8px; margin-top: 12px; color: var(--kb-text-faint); font-size: 12px; }

/* ---------- Доска ---------- */

.kb-board { display: flex; flex-direction: column; height: 100vh; }

.kb-board__head {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 14px 20px;
    border-bottom: 1px solid var(--kb-border);
    background: var(--kb-surface);
}

.kb-board__title { margin: 0; font-size: 17px; max-width: 320px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.kb-board__search { width: 240px; }
.kb-board__spacer { flex: 1; }
.kb-board__members { display: flex; }
.kb-board__members .kb-avatar { margin-left: -6px; border: 2px solid var(--kb-surface); }

.kb-columns { flex: 1; display: flex; gap: 12px; padding: 16px 20px; overflow-x: auto; align-items: flex-start; }

.kb-column {
    width: var(--kb-column-w);
    flex: 0 0 var(--kb-column-w);
    display: flex;
    flex-direction: column;
    max-height: 100%;
    background: var(--kb-surface-2);
    border: 1px solid transparent;
    border-radius: var(--kb-radius-lg);
}

.kb-column.is-over { border-color: var(--kb-accent); background: var(--kb-accent-soft); }
.kb-column__head { display: flex; align-items: center; gap: 8px; padding: 10px 12px 6px; font-size: 13px; font-weight: 600; }
.kb-column__count { color: var(--kb-text-faint); font-weight: 400; }
.kb-column__body { flex: 1; overflow-y: auto; padding: 4px 8px 8px; min-height: 60px; }
.kb-column__foot { padding: 4px 8px 8px; }

/* ---------- Карточка задачи ---------- */

.kb-card {
    padding: 10px 11px;
    margin-bottom: 8px;
    background: var(--kb-surface);
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius);
    box-shadow: var(--kb-shadow);
    cursor: pointer;
}

.kb-card:hover { border-color: var(--kb-accent); }
.kb-card.is-moving { opacity: .55; cursor: progress; }
.kb-card__name { margin-bottom: 8px; word-break: break-word; }
.kb-card__foot { display: flex; align-items: center; gap: 8px; }
.kb-card__counters { margin-left: auto; display: flex; gap: 8px; color: var(--kb-text-faint); font-size: 12px; }

.kb-badge {
    display: inline-block;
    padding: 1px 7px;
    border-radius: 10px;
    font-size: 11px;
    background: var(--kb-surface-3);
    color: var(--kb-text-muted);
}

.kb-badge--overdue { background: var(--kb-danger-soft); color: var(--kb-danger); font-weight: 600; }
.kb-badge--soon { background: var(--kb-warning-soft); color: var(--kb-warning); }

.kb-avatar {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    flex: 0 0 24px;
    border-radius: 50%;
    color: #fff;
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    user-select: none;
}

.kb-avatar--empty { background: var(--kb-surface-3); color: var(--kb-text-faint); }
.kb-avatar--lg { width: 30px; height: 30px; flex-basis: 30px; font-size: 13px; }

/* ---------- Панель задачи ---------- */

.kb-overlay {
    position: fixed;
    inset: 0;
    background: var(--kb-overlay);
    display: flex;
    justify-content: flex-end;
    z-index: 40;
}

.kb-panel {
    width: var(--kb-panel-w);
    min-width: 720px;
    max-width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    background: var(--kb-surface);
    border-left: 1px solid var(--kb-border);
    box-shadow: var(--kb-shadow-lg);
}

.kb-panel__head { display: flex; align-items: center; gap: 10px; padding: 12px 18px; border-bottom: 1px solid var(--kb-border); }
.kb-panel__id { color: var(--kb-text-faint); font-family: var(--kb-font-mono); font-size: 12px; }
.kb-panel__body { flex: 1; display: flex; min-height: 0; }
.kb-panel__left { flex: 1; min-width: 0; overflow: auto; padding: 18px; }
.kb-panel__right { width: 288px; flex: 0 0 288px; overflow: auto; padding: 18px; border-left: 1px solid var(--kb-border); background: var(--kb-surface-2); }

.kb-panel__title { width: 100%; font-size: 19px; font-weight: 600; border: 1px solid transparent; border-radius: var(--kb-radius); padding: 4px 6px; background: transparent; }
.kb-panel__title:hover { border-color: var(--kb-border); }
.kb-panel__title:focus { border-color: var(--kb-accent); background: var(--kb-surface); outline: none; }

.kb-prop { margin-bottom: 14px; }
.kb-prop__label { font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--kb-text-faint); margin-bottom: 5px; }
.kb-prop__value { display: flex; align-items: center; gap: 8px; }

.kb-section__title { margin: 22px 0 10px; font-size: 13px; font-weight: 700; text-transform: uppercase; letter-spacing: .5px; color: var(--kb-text-muted); }
.kb-section__title:first-child { margin-top: 0; }

.kb-files { list-style: none; margin: 0; padding: 0; }
.kb-file { display: flex; align-items: center; gap: 8px; padding: 5px 0; font-size: 13px; border-bottom: 1px solid var(--kb-border); }
.kb-file a { color: var(--kb-accent); text-decoration: none; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.kb-file a:hover { text-decoration: underline; }

.kb-history { list-style: none; margin: 0; padding: 0; }
.kb-history__item { display: flex; align-items: center; gap: 8px; padding: 6px 0; font-size: 13px; border-bottom: 1px solid var(--kb-border); }
.kb-history__date { margin-left: auto; color: var(--kb-text-faint); font-size: 12px; white-space: nowrap; }

/* ---------- Комментарии ---------- */

.kb-comment { display: flex; gap: 10px; padding: 12px 0; border-bottom: 1px solid var(--kb-border); }
.kb-comment.is-pending { opacity: .6; }
.kb-comment__body { flex: 1; min-width: 0; }
.kb-comment__head { display: flex; align-items: baseline; gap: 8px; margin-bottom: 4px; }
.kb-comment__author { font-weight: 600; }
.kb-comment__date { color: var(--kb-text-faint); font-size: 12px; }
.kb-comment__actions { margin-left: auto; display: flex; gap: 4px; }
.kb-comment__text { white-space: pre-wrap; word-break: break-word; }
.kb-comment-form { margin-top: 14px; }
.kb-comment-form__row { display: flex; align-items: center; gap: 10px; margin-top: 8px; }

/* ---------- Модалки ---------- */

.kb-modal-overlay {
    position: fixed;
    inset: 0;
    background: var(--kb-overlay);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 24px;
    z-index: 50;
}

.kb-modal {
    width: 100%;
    max-width: 520px;
    max-height: 86vh;
    display: flex;
    flex-direction: column;
    background: var(--kb-surface);
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius-lg);
    box-shadow: var(--kb-shadow-lg);
}

.kb-modal--wide { max-width: 620px; }
.kb-modal__head { display: flex; align-items: center; gap: 10px; padding: 14px 18px; border-bottom: 1px solid var(--kb-border); }
.kb-modal__title { margin: 0; font-size: 16px; flex: 1; }
.kb-modal__body { padding: 18px; overflow: auto; }
.kb-modal__foot { display: flex; justify-content: flex-end; gap: 8px; padding: 14px 18px; border-top: 1px solid var(--kb-border); }

.kb-row { display: flex; align-items: center; gap: 8px; padding: 8px 0; border-bottom: 1px solid var(--kb-border); }
.kb-row__main { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.kb-row--drag { cursor: grab; }
.kb-row--drag.is-over { box-shadow: inset 0 2px 0 var(--kb-accent); }

.kb-suggest { position: relative; }
.kb-suggest__list {
    position: absolute;
    left: 0; right: 0; top: 100%;
    z-index: 5;
    margin: 2px 0 0;
    padding: 4px;
    list-style: none;
    background: var(--kb-surface);
    border: 1px solid var(--kb-border);
    border-radius: var(--kb-radius);
    box-shadow: var(--kb-shadow);
    max-height: 200px;
    overflow: auto;
}
.kb-suggest__item { padding: 6px 8px; border-radius: var(--kb-radius); cursor: pointer; }
.kb-suggest__item:hover { background: var(--kb-surface-2); }

/* ---------- Тосты ---------- */

.kb-toasts { position: fixed; right: 18px; bottom: 18px; z-index: 100; display: flex; flex-direction: column; gap: 8px; }

.kb-toast {
    min-width: 240px;
    max-width: 360px;
    padding: 10px 14px;
    border-radius: var(--kb-radius);
    background: var(--kb-surface);
    border: 1px solid var(--kb-border);
    box-shadow: var(--kb-shadow-lg);
    cursor: pointer;
}

.kb-toast--error { border-color: var(--kb-danger); color: var(--kb-danger); background: var(--kb-danger-soft); }
.kb-toast--success { border-color: var(--kb-success); color: var(--kb-success); }

/* ---------- Мелочи ---------- */

.kb-empty { padding: 22px; text-align: center; color: var(--kb-text-faint); font-size: 13px; }
.kb-empty--sm { padding: 10px 4px; text-align: left; }
.kb-muted { color: var(--kb-text-muted); }
.kb-error-screen { padding: 60px 24px; text-align: center; color: var(--kb-text-muted); }

.kb-spinner {
    display: inline-block;
    width: 14px;
    height: 14px;
    border: 2px solid var(--kb-border-strong);
    border-top-color: var(--kb-accent);
    border-radius: 50%;
    animation: kb-spin .7s linear infinite;
}

@keyframes kb-spin { to { transform: rotate(360deg); } }

.kb-seg { display: flex; gap: 2px; padding: 2px; background: var(--kb-surface-2); border-radius: var(--kb-radius); }
.kb-seg__btn { flex: 1; padding: 4px 6px; font-size: 12px; border: none; border-radius: var(--kb-radius); background: transparent; color: var(--kb-text-muted); cursor: pointer; }
.kb-seg__btn.is-active { background: var(--kb-surface); color: var(--kb-text); font-weight: 600; }
```

- [ ] Проверить, что в стилях нет захардкоженных цветов мимо токенов (допустимы только `#fff`/`#ffffff` на акцентных кнопках и аватарах):
```bash
cd . && grep -nE "#[0-9a-fA-F]{3,6}" wwwroot/css/app.css | grep -v "#fff"
```
Ожидаемый вывод: пусто.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/css/app.css && git commit -m "Общие стили приложения"
```

---

### Task 4: Razor-оболочка SPA

**Files:**
- Modify: `./Views/Shared/_Layout.cshtml` (весь файл, 14 строк)
- Modify: `./Views/Home/Index.cshtml` (весь файл, 5 строк)
- Create: `./wwwroot/js/app.js` (смоук-версия, будет полностью заменена в Task 12)

- [ ] Полностью заменить содержимое `Views/Shared/_Layout.cshtml` на:
```html
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - KanbanBoard</title>
    <script>
        // Выставляем тему до загрузки модулей, чтобы не мигал светлый фон.
        // 'system' (и любое другое значение) — атрибут не ставим, работает @media prefers-color-scheme.
        (function () {
            try {
                var t = localStorage.getItem('kb.theme');
                if (t === 'dark' || t === 'light') {
                    document.documentElement.setAttribute('data-theme', t);
                }
            } catch (e) { /* localStorage недоступен — остаёмся на системной теме */ }
        })();
    </script>
    <link rel="stylesheet" href="/css/tokens.css" />
    <link rel="stylesheet" href="/css/app.css" />
</head>
<body>
    @RenderBody()
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

- [ ] Полностью заменить содержимое `Views/Home/Index.cshtml` на:
```html
@{
    ViewData["Title"] = "Доски";
}

<div id="app"></div>

@section Scripts {
    <script type="module" src="/js/app.js"></script>
}
```

- [ ] Создать каталоги для скриптов:
```bash
mkdir -p ./wwwroot/js/components
```

- [ ] Создать `wwwroot/js/app.js` со смоук-версией (в Task 12 файл будет заменён целиком):
```js
// Смоук-версия точки входа: проверяем, что вендоренный Vue грузится и монтируется.
// Полная версия (роутер + компоненты) появится в Task 12.
import { createApp } from '/lib/vue.esm-browser.prod.js';

createApp({
    template: '<div class="kb-boot">Каркас SPA работает</div>'
}).mount('#app');
```

- [ ] Собрать и запустить приложение:
```bash
cd . && dotnet build && dotnet run
```
Ожидаемый вывод сборки: `Build succeeded.`, `0 Error(s)`.

- [ ] Проверить в браузере: открыть `http://localhost:5110`. Ожидается: по центру серым текстом «Каркас SPA работает», фон — светло-серый (`#f4f5f7`). В консоли браузера (DevTools → Console) ошибок нет; во вкладке Network файлы `/css/tokens.css`, `/css/app.css`, `/js/app.js`, `/lib/vue.esm-browser.prod.js` отдаются со статусом 200.

- [ ] Проверить no-flash скрипт темы: в консоли браузера выполнить `localStorage.setItem('kb.theme','dark')` и нажать F5. Ожидается: фон сразу тёмный (`#16181d`), без белой вспышки; `document.documentElement.getAttribute('data-theme')` возвращает `"dark"`. Затем выполнить `localStorage.removeItem('kb.theme')` и F5 — атрибут `data-theme` отсутствует, тема соответствует системной. Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add Views/Shared/_Layout.cshtml Views/Home/Index.cshtml wwwroot/js/app.js && git commit -m "Razor-оболочка SPA с подключением css и модуля точки входа"
```

---

### Task 5: Строки интерфейса

**Files:**
- Create: `./wwwroot/js/strings.js`

- [ ] Создать `wwwroot/js/strings.js` со следующим содержимым:
```js
// Все строки интерфейса. Язык интерфейса — русский, других локалей не предполагается.
export const S = {
    common: {
        loading: 'Загрузка…',
        save: 'Сохранить',
        cancel: 'Отмена',
        create: 'Создать',
        add: 'Добавить',
        edit: 'Редактировать',
        remove: 'Удалить',
        close: 'Закрыть',
        confirm: 'Подтвердить',
        nothing: '—',
        yes: 'Да',
        no: 'Нет'
    },

    theme: {
        label: 'Тема',
        system: 'Системная',
        light: 'Светлая',
        dark: 'Тёмная'
    },

    auth: {
        title: 'KanbanBoard',
        subtitle: 'Вход в рабочее пространство',
        tabLogin: 'Вход',
        tabRegister: 'Регистрация',
        login: 'Логин',
        password: 'Пароль',
        submitLogin: 'Войти',
        submitRegister: 'Зарегистрироваться',
        logout: 'Выход'
    },

    boards: {
        title: 'Доски',
        create: '+ Новая доска',
        createTitle: 'Новая доска',
        name: 'Название доски',
        description: 'Описание',
        author: 'Автор',
        empty: 'Досок пока нет — создайте первую доску',
        sidebarTitle: 'Мои доски',
        sidebarEmpty: 'Нет досок'
    },

    board: {
        searchPlaceholder: 'Поиск по карточкам',
        settings: 'Настройки',
        members: 'Участники',
        addTask: '+ Задача',
        createTaskTitle: 'Новая задача',
        emptyColumn: 'В колонке нет задач',
        noColumns: 'В доске нет колонок — добавьте их в настройках доски',
        notFound: 'Доска недоступна',
        notFoundHint: 'Доска удалена или вас исключили из участников',
        noSearchResults: 'Ничего не найдено'
    },

    task: {
        name: 'Название',
        description: 'Описание',
        descriptionEmpty: 'Описание не заполнено',
        status: 'Статус',
        worker: 'Исполнитель',
        noWorker: 'Без исполнителя',
        author: 'Автор',
        deadline: 'Дедлайн',
        createdAt: 'Создана',
        attachments: 'Вложения задачи',
        noAttachments: 'Вложений нет',
        history: 'История статусов',
        noHistory: 'История пуста',
        upload: 'Загрузить файл',
        deleteConfirm: 'Удалить задачу? Действие необратимо.',
        deleted: 'Задача удалена'
    },

    comments: {
        title: 'Комментарии',
        empty: 'Комментариев пока нет',
        placeholder: 'Написать комментарий…',
        submit: 'Отправить',
        edited: 'изменён',
        attachFile: 'Файл',
        deleteConfirm: 'Удалить комментарий?'
    },

    settings: {
        title: 'Настройки доски',
        tabMembers: 'Участники',
        tabColumns: 'Колонки',
        tabAbout: 'О доске',
        owner: 'владелец',
        addMember: 'Добавить участника',
        memberLoginPlaceholder: 'Логин участника',
        readOnlyHint: 'Изменять настройки может только владелец доски',
        columnName: 'Название колонки',
        addColumn: 'Добавить колонку',
        columnsHint: 'Порядок колонок можно менять перетаскиванием',
        deleteBoard: 'Удалить доску',
        deleteBoardConfirm: 'Удалить доску вместе со всеми задачами? Действие необратимо.',
        boardDeleted: 'Доска удалена'
    },

    errors: {
        required: 'Поле обязательно',
        tooLong: (max) => `Не длиннее ${max} символов`,
        passwordShort: 'Пароль не короче 6 символов',
        validation: 'Проверьте правильность заполнения полей',
        badRequest: 'Проверьте правильность заполнения полей',
        unauthorized: 'Требуется вход',
        forbidden: 'Недостаточно прав',
        notFound: 'Не найдено',
        conflict: 'Действие невозможно',
        server: 'Что-то пошло не так',
        network: 'Нет связи с сервером',
        badCredentials: 'Неверный логин или пароль',
        loginTaken: 'Логин уже занят',
        columnNotEmpty: 'В колонке есть задачи',
        lastColumn: 'Нельзя удалить последнюю колонку',
        boardNotAvailable: 'Доска недоступна',
        taskNotFound: 'Задача не найдена',
        modalNotFound: 'Не найдено или недостаточно прав',
        moveFailed: 'Не удалось переместить задачу',
        moveColumnFailed: 'Не удалось переместить колонку',
        commentFailed: 'Не удалось отправить комментарий',
        uploadFailed: 'Не удалось загрузить файл',
        userNotFound: 'Пользователь не найден'
    }
};
```

- [ ] Проверить, что модуль синтаксически корректен и грузится в браузере: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить:
```js
const m = await import('/js/strings.js'); m.S.errors.tooLong(50)
```
Ожидаемый вывод: `'Не длиннее 50 символов'`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/strings.js && git commit -m "Строки интерфейса на русском"
```

---

### Task 6: Хелперы отображения

**Files:**
- Create: `./wwwroot/js/ui.js`

- [ ] Создать `wwwroot/js/ui.js` со следующим содержимым:
```js
// Чистые хелперы отображения. Состояния здесь нет — только форматирование.
import { S } from '/js/strings.js';

// Бек отдаёт DateTime; если Kind оказался Unspecified, суффикса Z в JSON не будет.
// Считаем такие даты UTC — иначе браузер сдвинет их на локальную зону.
export function parseDate(value) {
    if (!value) return null;
    const hasZone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(value);
    const d = new Date(hasZone ? value : value + 'Z');
    return isNaN(d.getTime()) ? null : d;
}

const dateFmt = new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
const dateTimeFmt = new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
});
const shortFmt = new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: 'short' });

export function formatDate(value) {
    const d = parseDate(value);
    return d ? dateFmt.format(d) : S.common.nothing;
}

export function formatDateTime(value) {
    const d = parseDate(value);
    return d ? dateTimeFmt.format(d) : S.common.nothing;
}

// Бейдж дедлайна: просрочен — красный, ближайшие сутки — жёлтый, иначе нейтральный.
export function deadlineInfo(value) {
    const d = parseDate(value);
    if (!d) return { text: '', state: 'none' };
    const diff = d.getTime() - Date.now();
    let state = 'normal';
    if (diff < 0) state = 'overdue';
    else if (diff < 24 * 60 * 60 * 1000) state = 'soon';
    const sameYear = d.getFullYear() === new Date().getFullYear();
    return { text: sameYear ? shortFmt.format(d) : dateFmt.format(d), state };
}

export function initials(login) {
    return (login || '?').trim().charAt(0) || '?';
}

// Детерминированный цвет аватара по логину: картинок на беке нет и не будет.
export function avatarColor(login) {
    const s = login || '';
    let h = 0;
    for (let i = 0; i < s.length; i++) {
        h = (h * 31 + s.charCodeAt(i)) % 360;
    }
    return `hsl(${h}, 52%, 46%)`;
}

export function avatarStyle(login) {
    return { background: avatarColor(login) };
}

// ISO с сервера -> значение для <input type="datetime-local"> (локальное время без зоны).
export function toLocalInputValue(value) {
    const d = parseDate(value);
    if (!d) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function formatSize(bytes) {
    if (!bytes && bytes !== 0) return '';
    if (bytes < 1024) return bytes + ' Б';
    if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + ' КБ';
    return (bytes / 1024 / 1024).toFixed(1) + ' МБ';
}
```

- [ ] Проверить хелперы в браузере: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить:
```js
const u = await import('/js/ui.js'); [u.formatDate('2026-09-05T12:00:00'), u.initials('pavel'), u.avatarColor('pavel') === u.avatarColor('pavel'), u.deadlineInfo('2020-01-01T00:00:00Z').state]
```
Ожидаемый вывод: `['05.09.2026', 'p', true, 'overdue']`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/ui.js && git commit -m "Хелперы отображения дат, дедлайнов и аватаров"
```

---

### Task 7: Клиент API

**Files:**
- Create: `./wwwroot/js/api.js`

- [ ] Создать `wwwroot/js/api.js` со следующим содержимым:
```js
// Единственное место общения с беком. Здесь же карантин особенностей API:
// два вида тел ошибок, обязательный UTC-Z в датах, дублирующая клиентская валидация длин.
import { S } from '/js/strings.js';

export const LIMITS = {
    taskName: 50,
    boardName: 200,
    columnName: 100,
    description: 3000,
    comment: 10000,
    login: 100,
    passwordMin: 6
};

export class ApiError extends Error {
    constructor(status, message, fieldErrors) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.message = message;
        this.fieldErrors = fieldErrors || {};
    }
}

let onUnauthorized = null;

// Обработчик 401 регистрирует app.js — так api.js не зависит от store.js и роутера.
export function setUnauthorizedHandler(fn) {
    onUnauthorized = fn;
}

function defaultMessage(status) {
    if (status === 400) return S.errors.badRequest;
    if (status === 401) return S.errors.unauthorized;
    if (status === 403) return S.errors.forbidden;
    if (status === 404) return S.errors.notFound;
    if (status === 409) return S.errors.conflict;
    if (status === 0) return S.errors.network;
    return S.errors.server;
}

// Валидационный 400 от [ApiController] — ValidationProblemDetails с errors[Поле][].
// Все остальные ручные ошибки бека — плоские строки (text/plain).
// Заголовки ProblemDetails английские, поэтому title игнорируем и берём свой русский текст.
async function normalizeError(response) {
    const status = response.status;
    const contentType = response.headers.get('content-type') || '';
    const fieldErrors = {};
    let message = '';

    if (contentType.includes('json')) {
        let body = null;
        try {
            body = await response.json();
        } catch (e) {
            body = null;
        }
        if (body && body.errors && typeof body.errors === 'object') {
            for (const key of Object.keys(body.errors)) {
                const field = key.charAt(0).toLowerCase() + key.slice(1);
                const value = body.errors[key];
                fieldErrors[field] = Array.isArray(value) ? value.join(' ') : String(value);
            }
            message = S.errors.validation;
        }
    } else {
        try {
            message = (await response.text()).trim();
        } catch (e) {
            message = '';
        }
    }

    if (!message) message = defaultMessage(status);
    return new ApiError(status, message, fieldErrors);
}

async function request(method, url, options) {
    const opts = options || {};
    const init = { method, credentials: 'same-origin', headers: {} };

    if (opts.formData) {
        init.body = opts.formData;
    } else if (opts.body !== undefined) {
        init.headers['Content-Type'] = 'application/json';
        init.body = JSON.stringify(opts.body);
    }

    let response;
    try {
        response = await fetch(url, init);
    } catch (e) {
        console.error('[api] network error', method, url, e);
        throw new ApiError(0, S.errors.network, {});
    }

    if (!response.ok) {
        const error = await normalizeError(response);
        console.error('[api]', method, url, error.status, error.message, error.fieldErrors);
        if (error.status === 401 && !opts.skipAuthRedirect && onUnauthorized) {
            onUnauthorized();
        }
        throw error;
    }

    if (response.status === 204) return null;
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('json')) return await response.json();
    return await response.text();
}

// --- Клиентская валидация: мгновенный UX, дублирует ограничения бека ---

function tooLong(value, max) {
    return (value || '').length > max ? S.errors.tooLong(max) : null;
}

function required(value) {
    return (value || '').trim() ? null : S.errors.required;
}

function assertValid(fieldErrors) {
    const cleaned = {};
    let has = false;
    for (const key of Object.keys(fieldErrors)) {
        if (fieldErrors[key]) {
            cleaned[key] = fieldErrors[key];
            has = true;
        }
    }
    if (has) throw new ApiError(400, S.errors.validation, cleaned);
}

// Deadline из <input type="datetime-local"> имеет Kind=Unspecified — Npgsql на таком падает 500.
// Отправляем строго UTC ISO-8601 с Z.
export function toUtcIso(localValue) {
    if (!localValue) return null;
    const d = new Date(localValue);
    if (isNaN(d.getTime())) return null;
    return d.toISOString();
}

const enc = encodeURIComponent;

export const api = {
    // --- auth ---
    me() {
        return request('GET', '/api/auth/me', { skipAuthRedirect: true });
    },
    login(login, password) {
        assertValid({ login: required(login) || tooLong(login, LIMITS.login), password: required(password) });
        return request('POST', '/api/auth/login', { body: { login, password }, skipAuthRedirect: true });
    },
    register(login, password) {
        assertValid({
            login: required(login) || tooLong(login, LIMITS.login),
            password: required(password) || ((password || '').length < LIMITS.passwordMin ? S.errors.passwordShort : null)
        });
        return request('POST', '/api/auth/register', { body: { login, password }, skipAuthRedirect: true });
    },
    logout() {
        return request('POST', '/api/auth/logout', { skipAuthRedirect: true });
    },

    // --- boards ---
    boards() {
        return request('GET', '/api/boards');
    },
    board(boardId) {
        return request('GET', `/api/boards/${boardId}`);
    },
    createBoard(name, description) {
        assertValid({
            name: required(name) || tooLong(name, LIMITS.boardName),
            description: tooLong(description, LIMITS.description)
        });
        return request('POST', '/api/boards', { body: { name, description: description || '' } });
    },
    updateBoard(boardId, name, description) {
        assertValid({
            name: required(name) || tooLong(name, LIMITS.boardName),
            description: tooLong(description, LIMITS.description)
        });
        return request('PUT', `/api/boards/${boardId}`, { body: { name, description: description || '' } });
    },
    deleteBoard(boardId) {
        return request('DELETE', `/api/boards/${boardId}`);
    },
    boardUsers(boardId) {
        return request('GET', `/api/boards/${boardId}/users`);
    },
    addBoardUser(boardId, login) {
        assertValid({ login: required(login) || tooLong(login, LIMITS.login) });
        return request('POST', `/api/boards/${boardId}/users`, { body: { login } });
    },
    removeBoardUser(boardId, userId) {
        return request('DELETE', `/api/boards/${boardId}/users/${userId}`);
    },
    searchUsers(query, limit) {
        return request('GET', `/api/users/search?query=${enc(query)}&limit=${limit || 10}`);
    },

    // --- statuses (колонки) ---
    statuses(boardId) {
        return request('GET', `/api/boards/${boardId}/statuses`);
    },
    createStatus(boardId, name) {
        assertValid({ name: required(name) || tooLong(name, LIMITS.columnName) });
        return request('POST', `/api/boards/${boardId}/statuses`, { body: { name } });
    },
    renameStatus(boardId, statusId, name) {
        assertValid({ name: required(name) || tooLong(name, LIMITS.columnName) });
        return request('PUT', `/api/boards/${boardId}/statuses/${statusId}`, { body: { name } });
    },
    deleteStatus(boardId, statusId) {
        return request('DELETE', `/api/boards/${boardId}/statuses/${statusId}`);
    },
    moveStatus(boardId, statusId, position) {
        return request('PATCH', `/api/boards/${boardId}/statuses/${statusId}/position`, { body: { position } });
    },

    // --- tasks ---
    tasks(boardId) {
        return request('GET', `/api/boards/${boardId}/tasks`);
    },
    task(boardId, taskId) {
        return request('GET', `/api/boards/${boardId}/tasks/${taskId}`);
    },
    // payload: { taskName, taskDescription, deadlineLocal, workerId }
    createTask(boardId, payload) {
        assertValid({
            taskName: required(payload.taskName) || tooLong(payload.taskName, LIMITS.taskName),
            taskDescription: tooLong(payload.taskDescription, LIMITS.description),
            deadline: required(payload.deadlineLocal)
        });
        return request('POST', `/api/boards/${boardId}/tasks`, {
            body: {
                taskName: payload.taskName,
                taskDescription: payload.taskDescription || null,
                deadline: toUtcIso(payload.deadlineLocal),
                workerId: payload.workerId || null
            }
        });
    },
    updateTask(boardId, taskId, payload) {
        assertValid({
            taskName: required(payload.taskName) || tooLong(payload.taskName, LIMITS.taskName),
            taskDescription: tooLong(payload.taskDescription, LIMITS.description),
            deadline: required(payload.deadlineLocal)
        });
        return request('PUT', `/api/boards/${boardId}/tasks/${taskId}`, {
            body: {
                taskName: payload.taskName,
                taskDescription: payload.taskDescription || null,
                deadline: toUtcIso(payload.deadlineLocal),
                workerId: payload.workerId || null
            }
        });
    },
    deleteTask(boardId, taskId) {
        return request('DELETE', `/api/boards/${boardId}/tasks/${taskId}`);
    },
    // Смена статуса из панели задачи. DnD пользуется только moveTask.
    changeTaskStatus(boardId, taskId, newStatusId) {
        return request('PATCH', `/api/boards/${boardId}/tasks/${taskId}/status`, { body: { newStatusId } });
    },
    // Position — 0-based индекс в целевой колонке ПОСЛЕ изъятия перемещаемой задачи.
    moveTask(boardId, taskId, statusId, position) {
        return request('PATCH', `/api/boards/${boardId}/tasks/${taskId}/position`, { body: { statusId, position } });
    },
    taskHistory(boardId, taskId) {
        return request('GET', `/api/boards/${boardId}/tasks/${taskId}/history`);
    },

    // --- comments ---
    comments(boardId, taskId) {
        return request('GET', `/api/boards/${boardId}/tasks/${taskId}/comments`);
    },
    createComment(boardId, taskId, text) {
        assertValid({ text: required(text) || tooLong(text, LIMITS.comment) });
        return request('POST', `/api/boards/${boardId}/tasks/${taskId}/comments`, { body: { text } });
    },
    updateComment(boardId, taskId, commentId, text) {
        assertValid({ text: required(text) || tooLong(text, LIMITS.comment) });
        return request('PUT', `/api/boards/${boardId}/tasks/${taskId}/comments/${commentId}`, { body: { text } });
    },
    deleteComment(boardId, taskId, commentId) {
        return request('DELETE', `/api/boards/${boardId}/tasks/${taskId}/comments/${commentId}`);
    },

    // --- attachments ---
    taskAttachments(boardId, taskId) {
        return request('GET', `/api/boards/${boardId}/tasks/${taskId}/attachments`);
    },
    uploadTaskAttachment(boardId, taskId, file) {
        const fd = new FormData();
        fd.append('file', file);
        return request('POST', `/api/boards/${boardId}/tasks/${taskId}/attachments`, { formData: fd });
    },
    uploadCommentAttachment(boardId, commentId, file) {
        const fd = new FormData();
        fd.append('file', file);
        return request('POST', `/api/boards/${boardId}/comments/${commentId}/attachments`, { formData: fd });
    },
    deleteAttachment(boardId, attachmentId) {
        return request('DELETE', `/api/boards/${boardId}/attachments/${attachmentId}`);
    },
    downloadUrl(boardId, attachmentId) {
        return `/api/boards/${boardId}/attachments/${attachmentId}/download`;
    }
};
```

- [ ] Проверить клиентскую валидацию и нормализацию 401 в браузере: запустить `dotnet run`, открыть `http://localhost:5110` в приватном окне (без куки), в консоли выполнить:
```js
const a = await import('/js/api.js');
try { await a.api.login('', ''); } catch (e) { console.log(e.status, e.fieldErrors); }
try { await a.api.boards(); } catch (e) { console.log(e.status, e.message); }
```
Ожидаемый вывод первой строки: `400 {login: 'Поле обязательно', password: 'Поле обязательно'}` (запроса в Network нет — проверка клиентская).
Ожидаемый вывод второй строки: `401 'Требуется вход'`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/api.js && git commit -m "Клиент API с нормализацией ошибок и клиентской валидацией"
```

---

### Task 8: Общее состояние

**Files:**
- Create: `./wwwroot/js/store.js`

`route` и `navigate` живут здесь, а не в `app.js`: их читают компоненты, и если бы они экспортировались из `app.js`, получился бы цикл импортов (app.js → компоненты → app.js). Разбор хеша (`parseHash`) остаётся в `app.js`.

- [ ] Создать `wwwroot/js/store.js` со следующим содержимым:
```js
// Общее реактивное состояние приложения. Локальное состояние компонентов сюда не тащим.
import { reactive } from '/lib/vue.esm-browser.prod.js';
import { api } from '/js/api.js';
import { S } from '/js/strings.js';

export const store = reactive({
    user: null,            // { userId, login, dateOfRegistration }

    boards: [],            // BoardResponse[]
    boardsLoaded: false,

    board: null,           // BoardResponse текущей доски
    statuses: [],          // StatusResponse[] отсортированы по (order, statusId)
    tasks: [],             // TaskResponse[]
    members: [],           // UserResponse[]
    boardLoading: false,
    boardError: null,
    search: '',

    task: null,            // открытая в панели задача
    taskLoading: false,

    draggingTaskId: null,  // id перетаскиваемой карточки
    movingTaskIds: [],     // карточки с PATCH /position в полёте: на карточку один запрос

    toasts: []
});

export const route = reactive({ name: 'loading', boardId: null, taskId: null });

export function navigate(hash) {
    if (location.hash === hash) return;
    location.hash = hash;
}

// --- Тосты ---

let toastSeq = 0;

export function pushToast(text, kind) {
    const id = ++toastSeq;
    store.toasts.push({ id, text, kind: kind || 'info' });
    setTimeout(() => removeToast(id), 5000);
}

export function removeToast(id) {
    const i = store.toasts.findIndex((t) => t.id === id);
    if (i >= 0) store.toasts.splice(i, 1);
}

// --- Сессия ---

export function resetAll() {
    store.user = null;
    store.boards = [];
    store.boardsLoaded = false;
    resetBoard();
    store.toasts = [];
}

export function resetBoard() {
    store.board = null;
    store.statuses = [];
    store.tasks = [];
    store.members = [];
    store.boardError = null;
    store.search = '';
    store.task = null;
    store.draggingTaskId = null;
    store.movingTaskIds = [];
}

// --- Доски ---

export async function loadBoards() {
    try {
        store.boards = await api.boards();
        store.boardsLoaded = true;
    } catch (e) {
        if (e.status !== 401) pushToast(e.message, 'error');
    }
}

export function upsertBoard(board) {
    const i = store.boards.findIndex((b) => b.boardId === board.boardId);
    if (i >= 0) store.boards[i] = board;
    else store.boards.push(board);
    if (store.board && store.board.boardId === board.boardId) store.board = board;
}

export function removeBoard(boardId) {
    const i = store.boards.findIndex((b) => b.boardId === boardId);
    if (i >= 0) store.boards.splice(i, 1);
    if (store.board && store.board.boardId === boardId) resetBoard();
}

// --- Доска ---

export async function loadBoard(boardId) {
    store.boardLoading = true;
    store.boardError = null;
    store.search = '';
    try {
        const [board, statuses, tasks, members] = await Promise.all([
            api.board(boardId),
            api.statuses(boardId),
            api.tasks(boardId),
            api.boardUsers(boardId)
        ]);
        store.board = board;
        store.statuses = statuses;
        store.tasks = tasks;
        store.members = members;
    } catch (e) {
        store.board = null;
        store.statuses = [];
        store.tasks = [];
        store.members = [];
        if (e.status !== 401) {
            store.boardError = e.status === 404 ? S.errors.boardNotAvailable : e.message;
        }
    } finally {
        store.boardLoading = false;
    }
}

export function isBoardOwner() {
    return !!(store.board && store.user && store.board.author && store.board.author.userId === store.user.userId);
}

export function sortStatuses() {
    store.statuses.sort((a, b) => (a.order - b.order) || (a.statusId - b.statusId));
}

export function applyStatusPositions(affected) {
    for (const item of affected || []) {
        const status = store.statuses.find((s) => s.statusId === item.id);
        if (status) status.order = item.order;
    }
    sortStatuses();
}

// --- Задачи ---

export function sortTasks(list) {
    return list.sort((a, b) => (a.order - b.order) || (a.taskId - b.taskId));
}

// Задачи колонки с учётом клиентского поиска (серверный ?search= не используем).
export function visibleTasks(statusId) {
    const query = store.search.trim().toLowerCase();
    const list = store.tasks.filter((t) => {
        if (t.status.statusId !== statusId) return false;
        if (!query) return true;
        const name = (t.taskName || '').toLowerCase();
        const desc = (t.taskDescription || '').toLowerCase();
        return name.includes(query) || desc.includes(query);
    });
    return sortTasks(list);
}

export function applyTaskUpdate(updated) {
    const i = store.tasks.findIndex((t) => t.taskId === updated.taskId);
    if (i >= 0) store.tasks[i] = updated;
    else store.tasks.push(updated);
    if (store.task && store.task.taskId === updated.taskId) store.task = updated;
}

export function removeTask(taskId) {
    const i = store.tasks.findIndex((t) => t.taskId === taskId);
    if (i >= 0) store.tasks.splice(i, 1);
    if (store.task && store.task.taskId === taskId) store.task = null;
}

export function bumpCounter(taskId, field, delta) {
    const task = store.tasks.find((t) => t.taskId === taskId);
    if (task) task[field] = Math.max(0, (task[field] || 0) + delta);
    if (store.task && store.task.taskId === taskId && store.task !== task) {
        store.task[field] = Math.max(0, (store.task[field] || 0) + delta);
    }
}
```

- [ ] Проверить, что стор грузится и тосты живут: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить:
```js
const s = await import('/js/store.js'); s.pushToast('проверка'); s.store.toasts.length
```
Ожидаемый вывод: `1`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/store.js && git commit -m "Общее реактивное состояние приложения"
```

---

### Task 9: Тосты и переключатель темы

**Files:**
- Create: `./wwwroot/js/components/Toast.js`
- Create: `./wwwroot/js/components/ThemeToggle.js`

- [ ] Создать `wwwroot/js/components/Toast.js`:
```js
import { store, removeToast } from '/js/store.js';

export default {
    name: 'Toast',
    setup() {
        return { store, removeToast };
    },
    template: `
        <div class="kb-toasts">
            <div v-for="t in store.toasts"
                 :key="t.id"
                 class="kb-toast"
                 :class="'kb-toast--' + t.kind"
                 @click="removeToast(t.id)">{{ t.text }}</div>
        </div>
    `
};
```

- [ ] Создать `wwwroot/js/components/ThemeToggle.js`:
```js
import { ref } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';

const THEME_KEY = 'kb.theme';

function readTheme() {
    try {
        const value = localStorage.getItem(THEME_KEY);
        return value === 'dark' || value === 'light' ? value : 'system';
    } catch (e) {
        return 'system';
    }
}

export default {
    name: 'ThemeToggle',
    setup() {
        const mode = ref(readTheme());

        // При system атрибут не ставим — работает @media prefers-color-scheme из tokens.css.
        function setMode(value) {
            mode.value = value;
            if (value === 'light' || value === 'dark') {
                document.documentElement.setAttribute('data-theme', value);
            } else {
                document.documentElement.removeAttribute('data-theme');
            }
            try {
                localStorage.setItem(THEME_KEY, value);
            } catch (e) {
                console.error('[theme] localStorage недоступен', e);
            }
        }

        return { S, mode, setMode };
    },
    template: `
        <div class="kb-seg" :title="S.theme.label">
            <button type="button" class="kb-seg__btn" :class="{ 'is-active': mode === 'system' }" @click="setMode('system')">{{ S.theme.system }}</button>
            <button type="button" class="kb-seg__btn" :class="{ 'is-active': mode === 'light' }" @click="setMode('light')">{{ S.theme.light }}</button>
            <button type="button" class="kb-seg__btn" :class="{ 'is-active': mode === 'dark' }" @click="setMode('dark')">{{ S.theme.dark }}</button>
        </div>
    `
};
```

- [ ] Проверить, что оба модуля импортируются без ошибок: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить:
```js
const [t, th] = await Promise.all([import('/js/components/Toast.js'), import('/js/components/ThemeToggle.js')]); [t.default.name, th.default.name]
```
Ожидаемый вывод: `['Toast', 'ThemeToggle']`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/Toast.js wwwroot/js/components/ThemeToggle.js && git commit -m "Компоненты тостов и переключателя темы"
```

---

### Task 10: Экран входа и регистрации

**Files:**
- Create: `./wwwroot/js/components/LoginPage.js`

- [ ] Создать `wwwroot/js/components/LoginPage.js`:
```js
import { ref } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, navigate, loadBoards } from '/js/store.js';

export default {
    name: 'LoginPage',
    setup() {
        const mode = ref('login');           // 'login' | 'register'
        const login = ref('');
        const password = ref('');
        const busy = ref(false);
        const formError = ref('');
        const fieldErrors = ref({});

        function switchMode(value) {
            mode.value = value;
            formError.value = '';
            fieldErrors.value = {};
        }

        async function submit() {
            if (busy.value) return;
            busy.value = true;
            formError.value = '';
            fieldErrors.value = {};
            try {
                if (mode.value === 'login') {
                    await api.login(login.value, password.value);
                } else {
                    await api.register(login.value, password.value);
                }
                store.user = await api.me();
                await loadBoards();
                navigate('#/');
            } catch (e) {
                fieldErrors.value = e.fieldErrors || {};
                if (e.status === 401) formError.value = S.errors.badCredentials;
                else if (e.status === 409) formError.value = e.message || S.errors.loginTaken;
                else if (e.status === 400 && Object.keys(fieldErrors.value).length === 0) formError.value = e.message;
                else if (e.status !== 400) formError.value = e.message;
            } finally {
                busy.value = false;
            }
        }

        return { S, LIMITS, mode, login, password, busy, formError, fieldErrors, switchMode, submit };
    },
    template: `
        <div class="kb-auth">
            <div class="kb-auth__card">
                <h1 class="kb-auth__title">{{ S.auth.title }}</h1>
                <p class="kb-auth__subtitle">{{ S.auth.subtitle }}</p>

                <div class="kb-tabs">
                    <button type="button" class="kb-tabs__item" :class="{ 'is-active': mode === 'login' }" @click="switchMode('login')">{{ S.auth.tabLogin }}</button>
                    <button type="button" class="kb-tabs__item" :class="{ 'is-active': mode === 'register' }" @click="switchMode('register')">{{ S.auth.tabRegister }}</button>
                </div>

                <form @submit.prevent="submit">
                    <div class="kb-field">
                        <label class="kb-field__label">{{ S.auth.login }}</label>
                        <input class="kb-input" v-model="login" :maxlength="LIMITS.login" autocomplete="username" />
                        <div v-if="fieldErrors.login" class="kb-field__error">{{ fieldErrors.login }}</div>
                    </div>
                    <div class="kb-field">
                        <label class="kb-field__label">{{ S.auth.password }}</label>
                        <input class="kb-input" type="password" v-model="password" :autocomplete="mode === 'login' ? 'current-password' : 'new-password'" />
                        <div v-if="fieldErrors.password" class="kb-field__error">{{ fieldErrors.password }}</div>
                    </div>

                    <button class="kb-btn kb-btn--primary" style="width:100%" type="submit" :disabled="busy">
                        <span v-if="busy" class="kb-spinner"></span>
                        <span>{{ mode === 'login' ? S.auth.submitLogin : S.auth.submitRegister }}</span>
                    </button>

                    <div v-if="formError" class="kb-form-error">{{ formError }}</div>
                </form>
            </div>
        </div>
    `
};
```

- [ ] Проверить импорт модуля: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить `(await import('/js/components/LoginPage.js')).default.name`. Ожидаемый вывод: `'LoginPage'`. Остановить приложение. (Полная проверка входа — в Task 12, когда роутер смонтирует компонент.)

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/LoginPage.js && git commit -m "Экран входа и регистрации"
```

---

### Task 11: Сайдбар и сетка досок

**Files:**
- Create: `./wwwroot/js/components/Sidebar.js`
- Create: `./wwwroot/js/components/BoardsGrid.js`

- [ ] Создать `wwwroot/js/components/Sidebar.js`:
```js
import { ref } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api } from '/js/api.js';
import { store, route, navigate, resetAll } from '/js/store.js';
import { initials, avatarStyle } from '/js/ui.js';
import ThemeToggle from '/js/components/ThemeToggle.js';

export default {
    name: 'Sidebar',
    components: { ThemeToggle },
    setup() {
        const busy = ref(false);

        async function logout() {
            if (busy.value) return;
            busy.value = true;
            try {
                await api.logout();
            } catch (e) {
                console.error('[sidebar] logout', e);
            } finally {
                busy.value = false;
                resetAll();
                navigate('#/login');
            }
        }

        return { S, store, route, initials, avatarStyle, busy, logout };
    },
    template: `
        <aside class="kb-sidebar">
            <div class="kb-sidebar__brand">
                <a class="kb-sidebar__item" href="#/" style="padding:0">{{ S.auth.title }}</a>
            </div>

            <div class="kb-sidebar__section">{{ S.boards.sidebarTitle }}</div>
            <nav class="kb-sidebar__list">
                <a v-for="b in store.boards"
                   :key="b.boardId"
                   class="kb-sidebar__item"
                   :class="{ 'is-active': route.boardId === b.boardId }"
                   :href="'#/boards/' + b.boardId">{{ b.nameOfBoard }}</a>
                <div v-if="store.boardsLoaded && store.boards.length === 0" class="kb-sidebar__empty">{{ S.boards.sidebarEmpty }}</div>
            </nav>

            <div class="kb-sidebar__footer">
                <ThemeToggle />
                <div class="kb-sidebar__user" v-if="store.user">
                    <span class="kb-avatar" :style="avatarStyle(store.user.login)">{{ initials(store.user.login) }}</span>
                    <span>{{ store.user.login }}</span>
                    <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" :disabled="busy" @click="logout">{{ S.auth.logout }}</button>
                </div>
            </div>
        </aside>
    `
};
```

- [ ] Создать `wwwroot/js/components/BoardsGrid.js`:
```js
import { ref } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, navigate, upsertBoard, pushToast } from '/js/store.js';
import { formatDate, initials, avatarStyle } from '/js/ui.js';

export default {
    name: 'BoardsGrid',
    setup() {
        const creating = ref(false);
        const name = ref('');
        const description = ref('');
        const busy = ref(false);
        const formError = ref('');
        const fieldErrors = ref({});

        function openCreate() {
            name.value = '';
            description.value = '';
            formError.value = '';
            fieldErrors.value = {};
            creating.value = true;
        }

        async function create() {
            if (busy.value) return;
            busy.value = true;
            formError.value = '';
            fieldErrors.value = {};
            try {
                const board = await api.createBoard(name.value, description.value);
                upsertBoard(board);
                creating.value = false;
                navigate('#/boards/' + board.boardId);
            } catch (e) {
                fieldErrors.value = e.fieldErrors || {};
                if (Object.keys(fieldErrors.value).length === 0 && e.status !== 401) formError.value = e.message;
            } finally {
                busy.value = false;
            }
        }

        return {
            S, LIMITS, store, formatDate, initials, avatarStyle,
            creating, name, description, busy, formError, fieldErrors, openCreate, create
        };
    },
    template: `
        <div class="kb-page">
            <div class="kb-page__head">
                <h1 class="kb-page__title">{{ S.boards.title }}</h1>
                <button type="button" class="kb-btn kb-btn--primary" @click="openCreate">{{ S.boards.create }}</button>
            </div>

            <div v-if="store.boardsLoaded && store.boards.length === 0" class="kb-empty">{{ S.boards.empty }}</div>

            <div class="kb-boards">
                <a v-for="b in store.boards" :key="b.boardId" class="kb-board-card" :href="'#/boards/' + b.boardId">
                    <div class="kb-board-card__name">{{ b.nameOfBoard }}</div>
                    <div class="kb-board-card__desc">{{ b.description }}</div>
                    <div class="kb-board-card__meta">
                        <span class="kb-avatar" :style="avatarStyle(b.author && b.author.login)">{{ initials(b.author && b.author.login) }}</span>
                        <span>{{ b.author && b.author.login }}</span>
                        <span>·</span>
                        <span>{{ formatDate(b.dateOfMade) }}</span>
                    </div>
                </a>
            </div>

            <div v-if="creating" class="kb-modal-overlay" @click.self="creating = false">
                <div class="kb-modal">
                    <div class="kb-modal__head">
                        <h2 class="kb-modal__title">{{ S.boards.createTitle }}</h2>
                        <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="creating = false">✕</button>
                    </div>
                    <div class="kb-modal__body">
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.boards.name }}</label>
                            <input class="kb-input" v-model="name" :maxlength="LIMITS.boardName" />
                            <div v-if="fieldErrors.name" class="kb-field__error">{{ fieldErrors.name }}</div>
                        </div>
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.boards.description }}</label>
                            <textarea class="kb-textarea" v-model="description" :maxlength="LIMITS.description"></textarea>
                            <div v-if="fieldErrors.description" class="kb-field__error">{{ fieldErrors.description }}</div>
                        </div>
                        <div v-if="formError" class="kb-form-error">{{ formError }}</div>
                    </div>
                    <div class="kb-modal__foot">
                        <button type="button" class="kb-btn" @click="creating = false">{{ S.common.cancel }}</button>
                        <button type="button" class="kb-btn kb-btn--primary" :disabled="busy" @click="create">{{ S.common.create }}</button>
                    </div>
                </div>
            </div>
        </div>
    `
};
```

- [ ] Проверить импорт обоих модулей: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить:
```js
const [s, g] = await Promise.all([import('/js/components/Sidebar.js'), import('/js/components/BoardsGrid.js')]); [s.default.name, g.default.name]
```
Ожидаемый вывод: `['Sidebar', 'BoardsGrid']`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/Sidebar.js wwwroot/js/components/BoardsGrid.js && git commit -m "Сайдбар со списком досок и сетка досок"
```

---

### Task 12: Роутер и сборка каркаса

**Files:**
- Modify: `./wwwroot/js/app.js` (полная замена смоук-версии из Task 4)

- [ ] Полностью заменить содержимое `wwwroot/js/app.js` на:
```js
// Точка входа: hash-роутер + корневой компонент + бутстрап сессии.
// Состояние маршрута (route/navigate) лежит в store.js, чтобы компоненты не импортировали app.js.
import { createApp, onMounted } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, setUnauthorizedHandler } from '/js/api.js';
import { store, route, navigate, resetAll, loadBoards } from '/js/store.js';
import Toast from '/js/components/Toast.js';
import LoginPage from '/js/components/LoginPage.js';
import Sidebar from '/js/components/Sidebar.js';
import BoardsGrid from '/js/components/BoardsGrid.js';

// #/login | #/ | #/boards/{id} | #/boards/{id}/tasks/{taskId}
function parseHash() {
    const hash = (location.hash || '').replace(/^#/, '') || '/';

    if (hash === '/login') {
        route.name = 'login';
        route.boardId = null;
        route.taskId = null;
        return;
    }

    const taskMatch = hash.match(/^\/boards\/(\d+)\/tasks\/(\d+)$/);
    if (taskMatch) {
        route.name = 'board';
        route.boardId = Number(taskMatch[1]);
        route.taskId = Number(taskMatch[2]);
        return;
    }

    const boardMatch = hash.match(/^\/boards\/(\d+)$/);
    if (boardMatch) {
        route.name = 'board';
        route.boardId = Number(boardMatch[1]);
        route.taskId = null;
        return;
    }

    route.name = 'boards';
    route.boardId = null;
    route.taskId = null;
}

window.addEventListener('hashchange', parseHash);

const App = {
    name: 'App',
    components: { Toast, LoginPage, Sidebar, BoardsGrid },
    setup() {
        onMounted(async () => {
            setUnauthorizedHandler(() => {
                resetAll();
                navigate('#/login');
            });

            parseHash();

            try {
                store.user = await api.me();
            } catch (e) {
                store.user = null;
                route.name = 'login';
                navigate('#/login');
                return;
            }

            if (route.name === 'login') {
                route.name = 'boards';
                navigate('#/');
            }
            await loadBoards();
        });

        return { S, store, route };
    },
    template: `
        <Toast />

        <LoginPage v-if="route.name === 'login'" />

        <div v-else-if="route.name === 'loading'" class="kb-boot">{{ S.common.loading }}</div>

        <div v-else class="kb-shell">
            <Sidebar />
            <main class="kb-main">
                <BoardsGrid v-if="route.name === 'boards'" />
                <div v-else class="kb-boot">{{ S.common.loading }}</div>
            </main>
        </div>
    `
};

createApp(App).mount('#app');
```

Маршрут `board` пока показывает «Загрузка…» — компонент `BoardView` появится в Task 16 и заменит этот `<div>`.

- [ ] Запустить приложение:
```bash
cd . && dotnet run
```

- [ ] Проверить регистрацию: открыть `http://localhost:5110` в приватном окне браузера. Ожидается: центрированная карточка «KanbanBoard / Вход в рабочее пространство» с вкладками «Вход»/«Регистрация», адрес меняется на `http://localhost:5110/#/login`. Переключиться на «Регистрация», ввести логин `pavel-front` и пароль `123456`, нажать «Зарегистрироваться». Ожидается: экран сменился на каркас — слева сайдбар «KanbanBoard / МОИ ДОСКИ / Нет досок», внизу переключатель темы и логин с кнопкой «Выход»; справа заголовок «Доски», кнопка «+ Новая доска» и текст «Досок пока нет — создайте первую доску»; адрес `#/`.

- [ ] Проверить короткий пароль: нажать «Выход», перейти на вкладку «Регистрация», ввести логин `x` и пароль `123`, отправить. Ожидается: под полем пароля красным «Пароль не короче 6 символов», запроса в Network нет.

- [ ] Проверить неверный пароль: вкладка «Вход», логин `pavel-front`, пароль `wrong-pass`, отправить. Ожидается: под формой красным «Неверный логин или пароль» (после фазы 1 бека логин отвечает 401; на неисправленном беке будет 409 и текст «Такой логин отсутствует» — тоже допустимо на этом шаге).

- [ ] Проверить занятый логин: вкладка «Регистрация», логин `pavel-front`, пароль `123456`. Ожидается: под формой «Логин уже занят».

- [ ] Проверить создание доски: войти как `pavel-front`/`123456`, нажать «+ Новая доска», ввести название `Фронт` и описание `Проверка каркаса`, нажать «Создать». Ожидается: модалка закрылась, адрес стал `#/boards/{id}`, в сайдбаре появилась доска «Фронт» и подсвечена активной, в основной области — «Загрузка…» (BoardView ещё нет — это ожидаемо на фазе 3).

- [ ] Проверить сетку и навигацию: кликнуть «KanbanBoard» в сайдбаре. Ожидается: адрес `#/`, карточка доски «Фронт» с описанием, аватаром-кружком с буквой `p`, логином автора и датой создания.

- [ ] Проверить темы: в сайдбаре нажать «Тёмная» — интерфейс сразу становится графитовым; F5 — тема не мигает. Нажать «Светлая» — белый строгий вид. Нажать «Системная» — тема совпадает с системной (проверить, переключив тему ОС или через DevTools → Rendering → Emulate CSS prefers-color-scheme).

- [ ] Проверить обработку 401: в консоли выполнить `document.cookie.split(';').forEach(c => document.cookie = c.split('=')[0] + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/')`, затем нажать «KanbanBoard» → перезагрузить F5. Ожидается: редирект на `#/login`. Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/app.js && git commit -m "Hash-роутер и сборка каркаса SPA"
```

---

### Task 13: Перемещение задач в сторе

**Files:**
- Modify: `./wwwroot/js/store.js` (добавление в конец файла)

- [ ] В конец `wwwroot/js/store.js` дописать блок оптимистичного перемещения задач:
```js
// --- Drag & drop: оптимистичное перемещение ---

export function isTaskMoving(taskId) {
    return store.movingTaskIds.includes(taskId);
}

function statusById(statusId) {
    return store.statuses.find((s) => s.statusId === statusId) || null;
}

function reindex(statusId) {
    const list = sortTasks(store.tasks.filter((t) => t.status.statusId === statusId));
    list.forEach((t, i) => { t.order = i; });
}

// Position — 0-based индекс в целевой колонке ПОСЛЕ изъятия перемещаемой задачи.
function applyLocalMove(task, targetStatusId, position) {
    const sourceStatusId = task.status.statusId;
    const target = statusById(targetStatusId);
    if (!target) return;

    const rest = sortTasks(store.tasks.filter((t) => t.status.statusId === targetStatusId && t.taskId !== task.taskId));
    const index = Math.max(0, Math.min(position, rest.length));

    task.status = target;
    rest.splice(index, 0, task);
    rest.forEach((t, i) => { t.order = i; });

    if (sourceStatusId !== targetStatusId) reindex(sourceStatusId);
}

// Ответ сервера: [{ id, statusId, order }] по всем затронутым задачам.
export function applyTaskPositions(affected) {
    for (const item of affected || []) {
        const task = store.tasks.find((t) => t.taskId === item.id);
        if (!task) continue;
        const status = statusById(item.statusId);
        if (status) task.status = status;
        task.order = item.order;
    }
}

export async function moveTask(taskId, targetStatusId, position) {
    if (!store.board) return;
    if (isTaskMoving(taskId)) return;                      // на карточку — один запрос в полёте

    const task = store.tasks.find((t) => t.taskId === taskId);
    if (!task) return;
    if (task.status.statusId === targetStatusId) {
        const current = sortTasks(store.tasks.filter((t) => t.status.statusId === targetStatusId));
        const currentIndex = current.findIndex((t) => t.taskId === taskId);
        const rest = current.length - 1;
        if (currentIndex === Math.max(0, Math.min(position, rest))) return;  // ничего не меняется
    }

    const snapshot = store.tasks.map((t) => ({ id: t.taskId, statusId: t.status.statusId, order: t.order }));

    applyLocalMove(task, targetStatusId, position);
    store.movingTaskIds.push(taskId);

    try {
        const affected = await api.moveTask(store.board.boardId, taskId, targetStatusId, position);
        applyTaskPositions(affected);
    } catch (e) {
        applyTaskPositions(snapshot);
        if (e.status !== 401) pushToast(S.errors.moveFailed, 'error');
    } finally {
        const i = store.movingTaskIds.indexOf(taskId);
        if (i >= 0) store.movingTaskIds.splice(i, 1);
    }
}
```

- [ ] Проверить, что модуль по-прежнему грузится и новые экспорты видны: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить:
```js
const s = await import('/js/store.js'); [typeof s.moveTask, typeof s.applyTaskPositions, s.isTaskMoving(1)]
```
Ожидаемый вывод: `['function', 'function', false]`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/store.js && git commit -m "Оптимистичное перемещение задач с откатом при ошибке"
```

---

### Task 14: Карточка задачи

**Files:**
- Create: `./wwwroot/js/components/TaskCard.js`

- [ ] Создать `wwwroot/js/components/TaskCard.js`:
```js
import { computed } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { store, navigate, isTaskMoving } from '/js/store.js';
import { deadlineInfo, initials, avatarStyle } from '/js/ui.js';

export default {
    name: 'TaskCard',
    props: {
        task: { type: Object, required: true }
    },
    setup(props) {
        const moving = computed(() => isTaskMoving(props.task.taskId));
        const deadline = computed(() => deadlineInfo(props.task.deadline));
        const worker = computed(() => props.task.worker || null);

        function open() {
            if (!store.board) return;
            navigate('#/boards/' + store.board.boardId + '/tasks/' + props.task.taskId);
        }

        function onDragStart(event) {
            if (moving.value) {
                event.preventDefault();
                return;
            }
            event.dataTransfer.effectAllowed = 'move';
            event.dataTransfer.setData('text/plain', String(props.task.taskId));
            store.draggingTaskId = props.task.taskId;
        }

        function onDragEnd() {
            store.draggingTaskId = null;
        }

        return { S, moving, deadline, worker, initials, avatarStyle, open, onDragStart, onDragEnd };
    },
    template: `
        <div class="kb-card"
             :class="{ 'is-moving': moving }"
             :data-task-id="task.taskId"
             :draggable="!moving"
             @click="open"
             @dragstart="onDragStart"
             @dragend="onDragEnd">
            <div class="kb-card__name">{{ task.taskName }}</div>
            <div class="kb-card__foot">
                <span class="kb-badge"
                      :class="{ 'kb-badge--overdue': deadline.state === 'overdue', 'kb-badge--soon': deadline.state === 'soon' }"
                      :title="S.task.deadline">{{ deadline.text }}</span>

                <span v-if="worker" class="kb-avatar" :style="avatarStyle(worker.login)" :title="worker.login">{{ initials(worker.login) }}</span>
                <span v-else class="kb-avatar kb-avatar--empty" :title="S.task.noWorker">?</span>

                <span class="kb-card__counters">
                    <span v-if="task.commentsCount">💬 {{ task.commentsCount }}</span>
                    <span v-if="task.attachmentsCount">📎 {{ task.attachmentsCount }}</span>
                </span>
            </div>
        </div>
    `
};
```

- [ ] Проверить импорт: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить `(await import('/js/components/TaskCard.js')).default.name`. Ожидаемый вывод: `'TaskCard'`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/TaskCard.js && git commit -m "Карточка задачи с бейджем дедлайна, аватаром и счётчиками"
```

---

### Task 15: Колонка доски с drop-зоной

**Files:**
- Create: `./wwwroot/js/components/TaskColumn.js`

- [ ] Создать `wwwroot/js/components/TaskColumn.js`:
```js
import { ref, computed } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { store, visibleTasks } from '/js/store.js';
import TaskCard from '/js/components/TaskCard.js';

export default {
    name: 'TaskColumn',
    components: { TaskCard },
    props: {
        status: { type: Object, required: true }
    },
    emits: ['move', 'create'],
    setup(props, { emit }) {
        const bodyEl = ref(null);
        const isOver = ref(false);

        const tasks = computed(() => visibleTasks(props.status.statusId));

        // Индекс вставки считаем по серединам карточек, исключая перетаскиваемую —
        // это и есть Position: индекс в целевой колонке после изъятия элемента.
        function calcIndex(event) {
            if (!bodyEl.value) return 0;
            const cards = Array.from(bodyEl.value.querySelectorAll('[data-task-id]'))
                .filter((el) => Number(el.dataset.taskId) !== store.draggingTaskId);
            for (let i = 0; i < cards.length; i++) {
                const rect = cards[i].getBoundingClientRect();
                if (event.clientY < rect.top + rect.height / 2) return i;
            }
            return cards.length;
        }

        function onDragOver(event) {
            if (store.draggingTaskId === null) return;
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
            isOver.value = true;
        }

        function onDragLeave(event) {
            if (bodyEl.value && event.relatedTarget && bodyEl.value.contains(event.relatedTarget)) return;
            isOver.value = false;
        }

        function onDrop(event) {
            event.preventDefault();
            isOver.value = false;
            const taskId = Number(event.dataTransfer.getData('text/plain'));
            if (!taskId) return;
            emit('move', { taskId, statusId: props.status.statusId, position: calcIndex(event) });
            store.draggingTaskId = null;
        }

        return { S, store, bodyEl, isOver, tasks, onDragOver, onDragLeave, onDrop, emit };
    },
    template: `
        <section class="kb-column"
                 :class="{ 'is-over': isOver }"
                 @dragover="onDragOver"
                 @dragleave="onDragLeave"
                 @drop="onDrop">
            <header class="kb-column__head">
                <span>{{ status.statusName }}</span>
                <span class="kb-column__count">{{ tasks.length }}</span>
            </header>

            <div class="kb-column__body" ref="bodyEl">
                <TaskCard v-for="t in tasks" :key="t.taskId" :task="t" />
                <div v-if="tasks.length === 0" class="kb-empty kb-empty--sm">
                    {{ store.search.trim() ? S.board.noSearchResults : S.board.emptyColumn }}
                </div>
            </div>

            <footer class="kb-column__foot">
                <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="$emit('create', status.statusId)">{{ S.board.addTask }}</button>
            </footer>
        </section>
    `
};
```

- [ ] Проверить импорт: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить `(await import('/js/components/TaskColumn.js')).default.name`. Ожидаемый вывод: `'TaskColumn'`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/TaskColumn.js && git commit -m "Колонка доски со списком задач и drop-зоной"
```

---

### Task 16: Экран доски

**Files:**
- Create: `./wwwroot/js/components/BoardView.js`
- Modify: `./wwwroot/js/app.js` (импорт и шаблон корневого компонента)

- [ ] Создать `wwwroot/js/components/BoardView.js`:
```js
import { ref, computed, watch } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, route, loadBoard, moveTask, applyTaskUpdate, pushToast } from '/js/store.js';
import { initials, avatarStyle } from '/js/ui.js';
import TaskColumn from '/js/components/TaskColumn.js';

export default {
    name: 'BoardView',
    components: { TaskColumn },
    props: {
        boardId: { type: Number, required: true }
    },
    setup(props) {
        const creating = ref(false);
        const form = ref({ taskName: '', taskDescription: '', deadlineLocal: '', workerId: '', statusId: null });
        const busy = ref(false);
        const formError = ref('');
        const fieldErrors = ref({});

        watch(() => props.boardId, (id) => { loadBoard(id); }, { immediate: true });

        const firstStatusId = computed(() => (store.statuses.length ? store.statuses[0].statusId : null));

        function openCreate(statusId) {
            form.value = {
                taskName: '',
                taskDescription: '',
                deadlineLocal: '',
                workerId: '',
                statusId: statusId || firstStatusId.value
            };
            formError.value = '';
            fieldErrors.value = {};
            creating.value = true;
        }

        async function createTask() {
            if (busy.value) return;
            busy.value = true;
            formError.value = '';
            fieldErrors.value = {};
            try {
                const created = await api.createTask(props.boardId, {
                    taskName: form.value.taskName,
                    taskDescription: form.value.taskDescription,
                    deadlineLocal: form.value.deadlineLocal,
                    workerId: form.value.workerId ? Number(form.value.workerId) : null
                });
                applyTaskUpdate(created);
                // Новая задача создаётся в левой колонке; если выбрана другая — переносим её туда.
                // changeTaskStatus возвращает StatusHistoryResponse, а не TaskResponse,
                // поэтому задачу после переноса перечитываем отдельным GET.
                if (form.value.statusId && created.status.statusId !== form.value.statusId) {
                    await api.changeTaskStatus(props.boardId, created.taskId, form.value.statusId);
                    const fresh = await api.task(props.boardId, created.taskId);
                    applyTaskUpdate(fresh);
                }
                creating.value = false;
            } catch (e) {
                fieldErrors.value = e.fieldErrors || {};
                if (Object.keys(fieldErrors.value).length === 0 && e.status !== 401) formError.value = e.message;
            } finally {
                busy.value = false;
            }
        }

        function onMove(payload) {
            moveTask(payload.taskId, payload.statusId, payload.position);
        }

        return {
            S, LIMITS, store, route, initials, avatarStyle,
            creating, form, busy, formError, fieldErrors, openCreate, createTask, onMove, pushToast
        };
    },
    template: `
        <div v-if="store.boardLoading && !store.board" class="kb-boot">{{ S.common.loading }}</div>

        <div v-else-if="store.boardError" class="kb-error-screen">
            <h2>{{ store.boardError }}</h2>
            <p class="kb-muted">{{ S.board.notFoundHint }}</p>
            <a class="kb-btn" href="#/">{{ S.boards.title }}</a>
        </div>

        <div v-else-if="store.board" class="kb-board">
            <header class="kb-board__head">
                <h1 class="kb-board__title" :title="store.board.nameOfBoard">{{ store.board.nameOfBoard }}</h1>
                <input class="kb-input kb-board__search" v-model="store.search" :placeholder="S.board.searchPlaceholder" />
                <div class="kb-board__spacer"></div>
                <div class="kb-board__members" :title="S.board.members">
                    <span v-for="m in store.members" :key="m.userId" class="kb-avatar" :style="avatarStyle(m.login)" :title="m.login">{{ initials(m.login) }}</span>
                </div>
                <button type="button" class="kb-btn kb-btn--sm" @click="pushToast(S.settings.title)">{{ S.board.settings }}</button>
            </header>

            <div class="kb-columns">
                <TaskColumn v-for="s in store.statuses"
                            :key="s.statusId"
                            :status="s"
                            @move="onMove"
                            @create="openCreate" />
                <div v-if="store.statuses.length === 0" class="kb-empty">{{ S.board.noColumns }}</div>
            </div>

            <div v-if="creating" class="kb-modal-overlay" @click.self="creating = false">
                <div class="kb-modal">
                    <div class="kb-modal__head">
                        <h2 class="kb-modal__title">{{ S.board.createTaskTitle }}</h2>
                        <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="creating = false">✕</button>
                    </div>
                    <div class="kb-modal__body">
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.task.name }}</label>
                            <input class="kb-input" v-model="form.taskName" :maxlength="LIMITS.taskName" />
                            <div v-if="fieldErrors.taskName" class="kb-field__error">{{ fieldErrors.taskName }}</div>
                        </div>
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.task.description }}</label>
                            <textarea class="kb-textarea" v-model="form.taskDescription" :maxlength="LIMITS.description"></textarea>
                            <div v-if="fieldErrors.taskDescription" class="kb-field__error">{{ fieldErrors.taskDescription }}</div>
                        </div>
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.task.deadline }}</label>
                            <input class="kb-input" type="datetime-local" v-model="form.deadlineLocal" />
                            <div v-if="fieldErrors.deadline" class="kb-field__error">{{ fieldErrors.deadline }}</div>
                        </div>
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.task.status }}</label>
                            <select class="kb-select" v-model="form.statusId">
                                <option v-for="s in store.statuses" :key="s.statusId" :value="s.statusId">{{ s.statusName }}</option>
                            </select>
                        </div>
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.task.worker }}</label>
                            <select class="kb-select" v-model="form.workerId">
                                <option value="">{{ S.task.noWorker }}</option>
                                <option v-for="m in store.members" :key="m.userId" :value="m.userId">{{ m.login }}</option>
                            </select>
                        </div>
                        <div v-if="formError" class="kb-form-error">{{ formError }}</div>
                    </div>
                    <div class="kb-modal__foot">
                        <button type="button" class="kb-btn" @click="creating = false">{{ S.common.cancel }}</button>
                        <button type="button" class="kb-btn kb-btn--primary" :disabled="busy" @click="createTask">{{ S.common.create }}</button>
                    </div>
                </div>
            </div>
        </div>
    `
};
```

- [ ] В `wwwroot/js/app.js` добавить импорт `BoardView`. Найти фрагмент:
```js
import BoardsGrid from '/js/components/BoardsGrid.js';
```
Заменить на:
```js
import BoardsGrid from '/js/components/BoardsGrid.js';
import BoardView from '/js/components/BoardView.js';
```

- [ ] В `wwwroot/js/app.js` зарегистрировать компонент. Найти фрагмент:
```js
    components: { Toast, LoginPage, Sidebar, BoardsGrid },
```
Заменить на:
```js
    components: { Toast, LoginPage, Sidebar, BoardsGrid, BoardView },
```

- [ ] В `wwwroot/js/app.js` подключить маршрут доски. Найти фрагмент:
```js
                <BoardsGrid v-if="route.name === 'boards'" />
                <div v-else class="kb-boot">{{ S.common.loading }}</div>
```
Заменить на:
```js
                <BoardsGrid v-if="route.name === 'boards'" />
                <BoardView v-else-if="route.name === 'board'" :key="route.boardId" :board-id="route.boardId" />
```

- [ ] Запустить приложение:
```bash
cd . && dotnet run
```

- [ ] Проверить экран доски: открыть `http://localhost:5110`, войти как `pavel-front`/`123456`, открыть доску «Фронт». Ожидается: шапка с названием доски, полем «Поиск по карточкам», аватаром участника справа и кнопкой «Настройки»; ниже — колонки доски (созданные беком по умолчанию), каждая с заголовком, счётчиком `0` и кнопкой «+ Задача»; в пустых колонках текст «В колонке нет задач».

- [ ] Проверить создание задач: в первой колонке нажать «+ Задача», заполнить название `Первая`, дедлайн — вчерашняя дата, «Создать». Ожидается: карточка появилась в первой колонке, бейдж дедлайна красный. Повторить с названием `Вторая` (дедлайн через месяц, исполнитель `pavel-front`) и `Третья` (дедлайн через час, без исполнителя). Ожидается: у «Второй» серый бейдж и цветной аватар с буквой `p`, у «Третьей» жёлтый бейдж и серый кружок `?`.

- [ ] Проверить обязательность дедлайна: «+ Задача», ввести только название, «Создать». Ожидается: под полем дедлайна «Поле обязательно», запроса в Network нет.

- [ ] Проверить DnD между колонками: перетащить карточку «Первая» во вторую колонку. Ожидается: во время перетаскивания вторая колонка подсвечивается рамкой акцентного цвета; карточка мгновенно оказывается в новой колонке; в Network проходит `PATCH /api/boards/{id}/tasks/{taskId}/position` со статусом 200; после F5 карточка остаётся в той же колонке.

- [ ] Проверить DnD внутри колонки: вернуть карточку в первую колонку и перетащить её выше/ниже соседних. Ожидается: порядок меняется сразу, `PATCH .../position` возвращает 200, после F5 порядок сохранён.

- [ ] Проверить откат при ошибке: в DevTools → Network включить `Offline`, перетащить карточку в соседнюю колонку. Ожидается: карточка сначала переезжает, затем возвращается на прежнее место, справа снизу появляется красный тост «Не удалось переместить задачу», в консоли — залогированная ошибка `[api] network error`. Выключить `Offline`.

- [ ] Проверить поиск: ввести в поле поиска `втор`. Ожидается: остаётся только карточка «Вторая», в остальных колонках текст «Ничего не найдено». Очистить поле — карточки вернулись.

- [ ] Проверить недоступную доску: открыть `http://localhost:5110/#/boards/999999`. Ожидается: экран «Доска недоступна» с подсказкой «Доска удалена или вас исключили из участников» и кнопкой «Доски». Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/BoardView.js wwwroot/js/app.js && git commit -m "Экран доски: шапка, поиск, колонки, создание задач и drag&drop"
```

---

### Task 17: Комментарии задачи

**Files:**
- Create: `./wwwroot/js/components/CommentList.js`

- [ ] Создать `wwwroot/js/components/CommentList.js`:
```js
import { ref, watch } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, pushToast, bumpCounter } from '/js/store.js';
import { formatDateTime, initials, avatarStyle } from '/js/ui.js';

export default {
    name: 'CommentList',
    props: {
        boardId: { type: Number, required: true },
        taskId: { type: Number, required: true }
    },
    setup(props) {
        const comments = ref([]);
        const loading = ref(false);
        const text = ref('');
        const file = ref(null);
        const fileInput = ref(null);
        const sending = ref(false);
        const error = ref('');
        const editingId = ref(null);
        const editingText = ref('');

        let tempSeq = 0;

        async function load() {
            loading.value = true;
            error.value = '';
            try {
                comments.value = await api.comments(props.boardId, props.taskId);
            } catch (e) {
                comments.value = [];
                if (e.status !== 401) error.value = e.message;
            } finally {
                loading.value = false;
            }
        }

        watch(() => props.taskId, load, { immediate: true });

        function onFileChange(event) {
            file.value = event.target.files && event.target.files[0] ? event.target.files[0] : null;
        }

        function clearFile() {
            file.value = null;
            if (fileInput.value) fileInput.value.value = '';
        }

        async function send() {
            if (sending.value || !text.value.trim()) return;
            error.value = '';
            const body = text.value;
            const attached = file.value;

            // Чисто текстовый коммент — оптимистично с tempId.
            // Коммент с вложением — обычный спиннер: файлу нужен серверный id комментария.
            if (!attached) {
                const tempId = 'temp-' + (++tempSeq);
                const optimistic = {
                    commentId: tempId,
                    taskId: props.taskId,
                    text: body,
                    madeDate: new Date().toISOString(),
                    isEdited: false,
                    author: store.user ? { userId: store.user.userId, login: store.user.login } : { userId: 0, login: '' },
                    attachments: [],
                    pending: true
                };
                comments.value.push(optimistic);
                text.value = '';
                try {
                    const created = await api.createComment(props.boardId, props.taskId, body);
                    const i = comments.value.findIndex((c) => c.commentId === tempId);
                    if (i >= 0) comments.value[i] = created;
                    bumpCounter(props.taskId, 'commentsCount', 1);
                } catch (e) {
                    const i = comments.value.findIndex((c) => c.commentId === tempId);
                    if (i >= 0) comments.value.splice(i, 1);
                    text.value = body;
                    if (e.status !== 401) pushToast(e.message || S.errors.commentFailed, 'error');
                }
                return;
            }

            sending.value = true;
            try {
                const created = await api.createComment(props.boardId, props.taskId, body);
                try {
                    await api.uploadCommentAttachment(props.boardId, created.commentId, attached);
                } catch (e) {
                    if (e.status !== 401) pushToast(S.errors.uploadFailed, 'error');
                }
                text.value = '';
                clearFile();
                bumpCounter(props.taskId, 'commentsCount', 1);
                await load();
            } catch (e) {
                if (e.status !== 401) pushToast(e.message || S.errors.commentFailed, 'error');
            } finally {
                sending.value = false;
            }
        }

        function startEdit(comment) {
            editingId.value = comment.commentId;
            editingText.value = comment.text;
        }

        async function saveEdit(comment) {
            try {
                const updated = await api.updateComment(props.boardId, props.taskId, comment.commentId, editingText.value);
                const i = comments.value.findIndex((c) => c.commentId === comment.commentId);
                if (i >= 0) comments.value[i] = updated;
                editingId.value = null;
            } catch (e) {
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        async function remove(comment) {
            if (!confirm(S.comments.deleteConfirm)) return;
            try {
                await api.deleteComment(props.boardId, props.taskId, comment.commentId);
                const i = comments.value.findIndex((c) => c.commentId === comment.commentId);
                if (i >= 0) comments.value.splice(i, 1);
                bumpCounter(props.taskId, 'commentsCount', -1);
            } catch (e) {
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        function isMine(comment) {
            return !!(store.user && comment.author && comment.author.userId === store.user.userId);
        }

        return {
            S, LIMITS, api, comments, loading, text, file, fileInput, sending, error,
            editingId, editingText, formatDateTime, initials, avatarStyle,
            onFileChange, clearFile, send, startEdit, saveEdit, remove, isMine
        };
    },
    template: `
        <div>
            <div class="kb-section__title">{{ S.comments.title }}</div>

            <div v-if="loading" class="kb-empty kb-empty--sm">{{ S.common.loading }}</div>
            <div v-else-if="error" class="kb-form-error">{{ error }}</div>
            <div v-else-if="comments.length === 0" class="kb-empty kb-empty--sm">{{ S.comments.empty }}</div>

            <div v-for="c in comments" :key="c.commentId" class="kb-comment" :class="{ 'is-pending': c.pending }">
                <span class="kb-avatar" :style="avatarStyle(c.author && c.author.login)">{{ initials(c.author && c.author.login) }}</span>
                <div class="kb-comment__body">
                    <div class="kb-comment__head">
                        <span class="kb-comment__author">{{ c.author && c.author.login }}</span>
                        <span class="kb-comment__date">{{ formatDateTime(c.madeDate) }}</span>
                        <span v-if="c.isEdited" class="kb-comment__date">({{ S.comments.edited }})</span>
                        <span v-if="isMine(c) && !c.pending" class="kb-comment__actions">
                            <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="startEdit(c)">{{ S.common.edit }}</button>
                            <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="remove(c)">{{ S.common.remove }}</button>
                        </span>
                    </div>

                    <div v-if="editingId === c.commentId">
                        <textarea class="kb-textarea" v-model="editingText" :maxlength="LIMITS.comment"></textarea>
                        <div class="kb-comment-form__row">
                            <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" @click="saveEdit(c)">{{ S.common.save }}</button>
                            <button type="button" class="kb-btn kb-btn--sm" @click="editingId = null">{{ S.common.cancel }}</button>
                        </div>
                    </div>
                    <div v-else class="kb-comment__text">{{ c.text }}</div>

                    <ul class="kb-files" v-if="c.attachments && c.attachments.length">
                        <li class="kb-file" v-for="a in c.attachments" :key="a.attachmentId">
                            <span>📎</span>
                            <a :href="api.downloadUrl(boardId, a.attachmentId)">{{ a.fileName }}</a>
                        </li>
                    </ul>
                </div>
            </div>

            <div class="kb-comment-form">
                <textarea class="kb-textarea" v-model="text" :maxlength="LIMITS.comment" :placeholder="S.comments.placeholder"></textarea>
                <div class="kb-comment-form__row">
                    <input type="file" ref="fileInput" @change="onFileChange" />
                    <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" :disabled="sending || !text.trim()" @click="send">
                        <span v-if="sending" class="kb-spinner"></span>
                        <span>{{ S.comments.submit }}</span>
                    </button>
                </div>
            </div>
        </div>
    `
};
```

- [ ] Проверить импорт: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить `(await import('/js/components/CommentList.js')).default.name`. Ожидаемый вывод: `'CommentList'`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/CommentList.js && git commit -m "Комментарии задачи с оптимистичной отправкой и вложениями"
```

---

### Task 18: Панель задачи

**Files:**
- Create: `./wwwroot/js/components/TaskPanel.js`

- [ ] Создать `wwwroot/js/components/TaskPanel.js`:
```js
import { ref, computed, watch } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, navigate, applyTaskUpdate, removeTask, pushToast, bumpCounter } from '/js/store.js';
import { formatDateTime, toLocalInputValue, initials, avatarStyle } from '/js/ui.js';
import CommentList from '/js/components/CommentList.js';

export default {
    name: 'TaskPanel',
    components: { CommentList },
    props: {
        boardId: { type: Number, required: true },
        task: { type: Object, required: true }
    },
    setup(props) {
        const name = ref('');
        const description = ref('');
        const deadlineLocal = ref('');
        const workerId = ref('');
        const statusId = ref(null);

        const saving = ref(false);
        const fieldErrors = ref({});
        const attachments = ref([]);
        const history = ref([]);
        const uploading = ref(false);
        const fileInput = ref(null);

        const isDirty = computed(() =>
            name.value !== props.task.taskName ||
            (description.value || '') !== (props.task.taskDescription || '') ||
            deadlineLocal.value !== toLocalInputValue(props.task.deadline) ||
            String(workerId.value || '') !== String(props.task.worker ? props.task.worker.userId : '')
        );

        function syncFromTask() {
            name.value = props.task.taskName;
            description.value = props.task.taskDescription || '';
            deadlineLocal.value = toLocalInputValue(props.task.deadline);
            workerId.value = props.task.worker ? props.task.worker.userId : '';
            statusId.value = props.task.status.statusId;
            fieldErrors.value = {};
        }

        async function loadSide() {
            const [files, hist] = await Promise.all([
                api.taskAttachments(props.boardId, props.task.taskId).catch((e) => {
                    if (e.status !== 401) console.error('[panel] attachments', e);
                    return [];
                }),
                api.taskHistory(props.boardId, props.task.taskId).catch((e) => {
                    if (e.status !== 401) console.error('[panel] history', e);
                    return [];
                })
            ]);
            attachments.value = files;
            history.value = hist;
        }

        watch(() => props.task.taskId, () => {
            syncFromTask();
            loadSide();
        }, { immediate: true });

        async function save() {
            if (saving.value) return;
            saving.value = true;
            fieldErrors.value = {};
            try {
                const updated = await api.updateTask(props.boardId, props.task.taskId, {
                    taskName: name.value,
                    taskDescription: description.value,
                    deadlineLocal: deadlineLocal.value,
                    workerId: workerId.value ? Number(workerId.value) : null
                });
                applyTaskUpdate(updated);
            } catch (e) {
                fieldErrors.value = e.fieldErrors || {};
                if (Object.keys(fieldErrors.value).length === 0 && e.status !== 401) pushToast(e.message, 'error');
            } finally {
                saving.value = false;
            }
        }

        async function changeStatus() {
            if (!statusId.value || statusId.value === props.task.status.statusId) return;
            try {
                // changeTaskStatus возвращает StatusHistoryResponse — задачу перечитываем отдельным GET.
                await api.changeTaskStatus(props.boardId, props.task.taskId, Number(statusId.value));
                const fresh = await api.task(props.boardId, props.task.taskId);
                applyTaskUpdate(fresh);
                history.value = await api.taskHistory(props.boardId, props.task.taskId);
            } catch (e) {
                statusId.value = props.task.status.statusId;
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        async function upload(event) {
            const file = event.target.files && event.target.files[0];
            if (!file) return;
            uploading.value = true;
            try {
                const created = await api.uploadTaskAttachment(props.boardId, props.task.taskId, file);
                attachments.value.push(created);
                bumpCounter(props.task.taskId, 'attachmentsCount', 1);
            } catch (e) {
                if (e.status !== 401) pushToast(S.errors.uploadFailed, 'error');
            } finally {
                uploading.value = false;
                if (fileInput.value) fileInput.value.value = '';
            }
        }

        async function removeAttachment(attachment) {
            try {
                await api.deleteAttachment(props.boardId, attachment.attachmentId);
                const i = attachments.value.findIndex((a) => a.attachmentId === attachment.attachmentId);
                if (i >= 0) attachments.value.splice(i, 1);
                bumpCounter(props.task.taskId, 'attachmentsCount', -1);
            } catch (e) {
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        async function removeCurrentTask() {
            if (!confirm(S.task.deleteConfirm)) return;
            try {
                await api.deleteTask(props.boardId, props.task.taskId);
                removeTask(props.task.taskId);
                pushToast(S.task.deleted, 'success');
                navigate('#/boards/' + props.boardId);
            } catch (e) {
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        function close() {
            navigate('#/boards/' + props.boardId);
        }

        return {
            S, LIMITS, api, store, name, description, deadlineLocal, workerId, statusId,
            saving, fieldErrors, attachments, history, uploading, fileInput, isDirty,
            formatDateTime, initials, avatarStyle,
            save, changeStatus, upload, removeAttachment, removeCurrentTask, close
        };
    },
    template: `
        <div class="kb-overlay" @click.self="close">
            <div class="kb-panel">
                <header class="kb-panel__head">
                    <span class="kb-panel__id">#{{ task.taskId }}</span>
                    <div class="kb-board__spacer"></div>
                    <button type="button" class="kb-btn kb-btn--sm" :disabled="!isDirty || saving" @click="save">
                        <span v-if="saving" class="kb-spinner"></span>
                        <span>{{ S.common.save }}</span>
                    </button>
                    <button type="button" class="kb-btn kb-btn--sm kb-btn--danger" @click="removeCurrentTask">{{ S.common.remove }}</button>
                    <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="close">✕</button>
                </header>

                <div class="kb-panel__body">
                    <div class="kb-panel__left">
                        <input class="kb-panel__title" v-model="name" :maxlength="LIMITS.taskName" />
                        <div v-if="fieldErrors.taskName" class="kb-field__error">{{ fieldErrors.taskName }}</div>

                        <div class="kb-section__title">{{ S.task.description }}</div>
                        <textarea class="kb-textarea" v-model="description" :maxlength="LIMITS.description" :placeholder="S.task.descriptionEmpty"></textarea>
                        <div v-if="fieldErrors.taskDescription" class="kb-field__error">{{ fieldErrors.taskDescription }}</div>

                        <CommentList :board-id="boardId" :task-id="task.taskId" />
                    </div>

                    <aside class="kb-panel__right">
                        <div class="kb-prop">
                            <div class="kb-prop__label">{{ S.task.status }}</div>
                            <select class="kb-select" v-model="statusId" @change="changeStatus">
                                <option v-for="s in store.statuses" :key="s.statusId" :value="s.statusId">{{ s.statusName }}</option>
                            </select>
                        </div>

                        <div class="kb-prop">
                            <div class="kb-prop__label">{{ S.task.worker }}</div>
                            <select class="kb-select" v-model="workerId">
                                <option value="">{{ S.task.noWorker }}</option>
                                <option v-for="m in store.members" :key="m.userId" :value="m.userId">{{ m.login }}</option>
                            </select>
                        </div>

                        <div class="kb-prop">
                            <div class="kb-prop__label">{{ S.task.deadline }}</div>
                            <input class="kb-input" type="datetime-local" v-model="deadlineLocal" />
                            <div v-if="fieldErrors.deadline" class="kb-field__error">{{ fieldErrors.deadline }}</div>
                        </div>

                        <div class="kb-prop">
                            <div class="kb-prop__label">{{ S.task.author }}</div>
                            <div class="kb-prop__value">
                                <span class="kb-avatar" :style="avatarStyle(task.author && task.author.login)">{{ initials(task.author && task.author.login) }}</span>
                                <span>{{ task.author && task.author.login }}</span>
                            </div>
                        </div>

                        <div class="kb-prop">
                            <div class="kb-prop__label">{{ S.task.createdAt }}</div>
                            <div class="kb-prop__value">{{ formatDateTime(task.dateOfMade) }}</div>
                        </div>

                        <div class="kb-section__title">{{ S.task.attachments }}</div>
                        <ul class="kb-files">
                            <li class="kb-file" v-for="a in attachments" :key="a.attachmentId">
                                <span>📎</span>
                                <a :href="api.downloadUrl(boardId, a.attachmentId)">{{ a.fileName }}</a>
                                <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" style="margin-left:auto" @click="removeAttachment(a)">✕</button>
                            </li>
                        </ul>
                        <div v-if="attachments.length === 0" class="kb-empty kb-empty--sm">{{ S.task.noAttachments }}</div>
                        <div class="kb-comment-form__row">
                            <input type="file" ref="fileInput" @change="upload" />
                            <span v-if="uploading" class="kb-spinner"></span>
                        </div>

                        <div class="kb-section__title">{{ S.task.history }}</div>
                        <ul class="kb-history">
                            <li class="kb-history__item" v-for="h in history" :key="h.statusChangeId">
                                <span class="kb-avatar" :style="avatarStyle(h.author && h.author.login)">{{ initials(h.author && h.author.login) }}</span>
                                <span>{{ h.status && h.status.statusName }}</span>
                                <span class="kb-history__date">{{ formatDateTime(h.lastStatusChangeDate) }}</span>
                            </li>
                        </ul>
                        <div v-if="history.length === 0" class="kb-empty kb-empty--sm">{{ S.task.noHistory }}</div>
                    </aside>
                </div>
            </div>
        </div>
    `
};
```

- [ ] Проверить импорт: запустить `dotnet run`, открыть `http://localhost:5110`, в консоли выполнить `(await import('/js/components/TaskPanel.js')).default.name`. Ожидаемый вывод: `'TaskPanel'`. Остановить приложение.

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/TaskPanel.js && git commit -m "Панель задачи в две колонки: поля, статус, вложения, история"
```

---

### Task 19: Подключение панели и deep-link на задачу

**Files:**
- Modify: `./wwwroot/js/store.js` (добавление в конец файла)
- Modify: `./wwwroot/js/components/BoardView.js` (импорты, setup, шаблон)

- [ ] В конец `wwwroot/js/store.js` дописать открытие задачи по deep-link:
```js
// --- Deep-link на задачу ---

// Если доска уже загружена — берём задачу из стора. При прямом заходе/F5 — грузим отдельно,
// не дожидаясь загрузки доски. 404 → тост + возврат на доску.
export async function openTask(boardId, taskId) {
    const local = store.tasks.find((t) => t.taskId === taskId);
    if (local) {
        store.task = local;
        store.taskLoading = false;
        return;
    }

    store.taskLoading = true;
    store.task = null;
    try {
        store.task = await api.task(boardId, taskId);
    } catch (e) {
        if (e.status === 404) {
            pushToast(S.errors.taskNotFound, 'error');
            navigate('#/boards/' + boardId);
        } else if (e.status !== 401) {
            pushToast(e.message, 'error');
        }
    } finally {
        store.taskLoading = false;
    }
}

export function closeTask() {
    store.task = null;
    store.taskLoading = false;
}
```

- [ ] В `wwwroot/js/components/BoardView.js` добавить импорты. Найти фрагмент:
```js
import { store, route, loadBoard, moveTask, applyTaskUpdate, pushToast } from '/js/store.js';
import { initials, avatarStyle } from '/js/ui.js';
import TaskColumn from '/js/components/TaskColumn.js';
```
Заменить на:
```js
import { store, route, loadBoard, moveTask, applyTaskUpdate, pushToast, openTask, closeTask } from '/js/store.js';
import { initials, avatarStyle } from '/js/ui.js';
import TaskColumn from '/js/components/TaskColumn.js';
import TaskPanel from '/js/components/TaskPanel.js';
```

- [ ] В `wwwroot/js/components/BoardView.js` зарегистрировать компонент. Найти фрагмент:
```js
    components: { TaskColumn },
```
Заменить на:
```js
    components: { TaskColumn, TaskPanel },
```

- [ ] В `wwwroot/js/components/BoardView.js` добавить реакцию на `route.taskId`. Найти фрагмент:
```js
        watch(() => props.boardId, (id) => { loadBoard(id); }, { immediate: true });
```
Заменить на:
```js
        watch(() => props.boardId, (id) => { loadBoard(id); }, { immediate: true });

        // Панель открывается по taskId из роута; при прямом заходе задача грузится отдельным запросом.
        watch(() => route.taskId, (taskId) => {
            if (taskId) openTask(props.boardId, taskId);
            else closeTask();
        }, { immediate: true });

        // Доска догрузилась после deep-link — берём задачу из стора, чтобы панель и карточка были одним объектом.
        watch(() => store.tasks.length, () => {
            if (route.taskId && store.task && store.task.taskId === route.taskId) {
                const local = store.tasks.find((t) => t.taskId === route.taskId);
                if (local && local !== store.task) store.task = local;
            }
        });
```

- [ ] В `wwwroot/js/components/BoardView.js` отрисовать панель. Найти фрагмент:
```js
            <div v-if="creating" class="kb-modal-overlay" @click.self="creating = false">
```
Заменить на:
```js
            <TaskPanel v-if="store.task" :key="store.task.taskId" :board-id="boardId" :task="store.task" />

            <div v-if="creating" class="kb-modal-overlay" @click.self="creating = false">
```

- [ ] Запустить приложение:
```bash
cd . && dotnet run
```

- [ ] Проверить открытие панели: открыть `http://localhost:5110`, войти, открыть доску «Фронт», кликнуть карточку «Вторая». Ожидается: адрес стал `#/boards/{id}/tasks/{taskId}`; поверх доски справа — широкая панель (примерно 58% ширины экрана) в две колонки: слева `#id`, название в виде редактируемого поля, описание и блок «КОММЕНТАРИИ» с текстом «Комментариев пока нет»; справа — «СТАТУС» (select), «ИСПОЛНИТЕЛЬ», «ДЕДЛАЙН», «АВТОР», «СОЗДАНА», «ВЛОЖЕНИЯ ЗАДАЧИ» («Вложений нет») и «ИСТОРИЯ СТАТУСОВ» с одной записью — созданием задачи.

- [ ] Проверить редактирование: изменить название на `Вторая (правка)`, описание — на `Проверка PUT`, нажать «Сохранить». Ожидается: кнопка «Сохранить» была неактивной до правки и стала активной после; в Network проходит `PUT /api/boards/{id}/tasks/{taskId}` со статусом 200; название карточки под панелью изменилось; после F5 правки на месте.

- [ ] Проверить смену статуса: в селекте «СТАТУС» выбрать другую колонку. Ожидается: `PATCH .../status` 200, карточка под панелью переехала в выбранную колонку, в «ИСТОРИИ СТАТУСОВ» сверху появилась новая запись с новым статусом, датой и аватаром автора (свежие сверху).

- [ ] Проверить комментарий без файла (оптимистичный): написать `Привет`, «Отправить». Ожидается: комментарий появляется мгновенно полупрозрачным, затем становится обычным; счётчик 💬 на карточке под панелью стал `1`.

- [ ] Проверить комментарий с файлом: написать `С файлом`, выбрать любой небольшой файл, «Отправить». Ожидается: на кнопке крутится спиннер до ответа; после — комментарий со строкой «📎 имя_файла»; клик по имени скачивает файл.

- [ ] Проверить вложение задачи: в правой колонке выбрать файл через input. Ожидается: файл появляется в списке «ВЛОЖЕНИЯ ЗАДАЧИ», счётчик 📎 на карточке стал `1`; нажать ✕ — файл исчезает, счётчик возвращается к нулю.

- [ ] Проверить deep-link по F5: находясь на `#/boards/{id}/tasks/{taskId}`, нажать F5. Ожидается: панель открывается, не дожидаясь полной загрузки доски (колонки появляются следом); данные задачи корректные.

- [ ] Проверить 404 задачи: открыть `http://localhost:5110/#/boards/{id}/tasks/999999`. Ожидается: красный тост «Задача не найдена» справа снизу и редирект на `#/boards/{id}`, экран доски цел.

- [ ] Проверить закрытие: нажать ✕ в панели или кликнуть по затемнению слева. Ожидается: панель закрылась, адрес стал `#/boards/{id}`. Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/store.js wwwroot/js/components/BoardView.js && git commit -m "Подключение панели задачи и обработка deep-link"
```

---

### Task 20: Настройки доски — участники и «О доске»

**Files:**
- Create: `./wwwroot/js/components/BoardSettingsModal.js`
- Modify: `./wwwroot/js/components/BoardView.js` (импорты, setup, шаблон, кнопка «Настройки»)

- [ ] Создать `wwwroot/js/components/BoardSettingsModal.js` (вкладка «Колонки» добавляется в Task 21):
```js
import { ref, computed } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, isBoardOwner, upsertBoard, removeBoard, navigate, pushToast, loadBoard } from '/js/store.js';
import { initials, avatarStyle } from '/js/ui.js';

export default {
    name: 'BoardSettingsModal',
    emits: ['close'],
    setup(props, { emit }) {
        const tab = ref('members');
        const error = ref('');           // ошибки настроек показываем внутри модалки, доску не роняем
        const busy = ref(false);

        const owner = computed(() => isBoardOwner());
        const boardId = computed(() => (store.board ? store.board.boardId : 0));

        // --- Участники ---
        const memberLogin = ref('');
        const suggestions = ref([]);

        function isOwnerUser(user) {
            return !!(store.board && store.board.author && store.board.author.userId === user.userId);
        }

        async function searchMembers() {
            const query = memberLogin.value.trim();
            if (query.length < 2) {
                suggestions.value = [];
                return;
            }
            try {
                suggestions.value = await api.searchUsers(query, 8);
            } catch (e) {
                suggestions.value = [];
            }
        }

        function pickSuggestion(user) {
            memberLogin.value = user.login;
            suggestions.value = [];
        }

        async function addMember() {
            if (busy.value) return;
            busy.value = true;
            error.value = '';
            try {
                await api.addBoardUser(boardId.value, memberLogin.value);
                memberLogin.value = '';
                suggestions.value = [];
                store.members = await api.boardUsers(boardId.value);
            } catch (e) {
                error.value = e.status === 404 ? S.errors.userNotFound : e.message;
            } finally {
                busy.value = false;
            }
        }

        async function removeMember(user) {
            if (busy.value) return;
            busy.value = true;
            error.value = '';
            try {
                await api.removeBoardUser(boardId.value, user.userId);
                store.members = await api.boardUsers(boardId.value);
                await loadBoard(boardId.value);   // у задач мог обнулиться исполнитель
            } catch (e) {
                error.value = e.status === 404 ? S.errors.modalNotFound : e.message;
            } finally {
                busy.value = false;
            }
        }

        // --- О доске ---
        const boardName = ref(store.board ? store.board.nameOfBoard : '');
        const boardDescription = ref(store.board ? store.board.description || '' : '');
        const fieldErrors = ref({});

        async function saveBoard() {
            if (busy.value) return;
            busy.value = true;
            error.value = '';
            fieldErrors.value = {};
            try {
                const updated = await api.updateBoard(boardId.value, boardName.value, boardDescription.value);
                upsertBoard(updated);
            } catch (e) {
                fieldErrors.value = e.fieldErrors || {};
                if (Object.keys(fieldErrors.value).length === 0) {
                    error.value = e.status === 404 ? S.errors.modalNotFound : e.message;
                }
            } finally {
                busy.value = false;
            }
        }

        async function deleteBoard() {
            if (!confirm(S.settings.deleteBoardConfirm)) return;
            busy.value = true;
            error.value = '';
            try {
                const id = boardId.value;
                await api.deleteBoard(id);
                emit('close');
                removeBoard(id);
                pushToast(S.settings.boardDeleted, 'success');
                navigate('#/');
            } catch (e) {
                error.value = e.status === 404 ? S.errors.modalNotFound : e.message;
            } finally {
                busy.value = false;
            }
        }

        return {
            S, LIMITS, store, tab, error, busy, owner,
            memberLogin, suggestions, searchMembers, pickSuggestion, addMember, removeMember, isOwnerUser,
            boardName, boardDescription, fieldErrors, saveBoard, deleteBoard,
            initials, avatarStyle
        };
    },
    template: `
        <div class="kb-modal-overlay" @click.self="$emit('close')">
            <div class="kb-modal kb-modal--wide">
                <div class="kb-modal__head">
                    <h2 class="kb-modal__title">{{ S.settings.title }}</h2>
                    <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="$emit('close')">✕</button>
                </div>

                <div class="kb-modal__body">
                    <div class="kb-tabs">
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'members' }" @click="tab = 'members'">{{ S.settings.tabMembers }}</button>
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'about' }" @click="tab = 'about'">{{ S.settings.tabAbout }}</button>
                    </div>

                    <div v-if="!owner" class="kb-empty kb-empty--sm">{{ S.settings.readOnlyHint }}</div>
                    <div v-if="error" class="kb-form-error">{{ error }}</div>

                    <div v-if="tab === 'members'">
                        <div class="kb-row" v-for="m in store.members" :key="m.userId">
                            <span class="kb-avatar" :style="avatarStyle(m.login)">{{ initials(m.login) }}</span>
                            <span class="kb-row__main">{{ m.login }}</span>
                            <span v-if="isOwnerUser(m)" class="kb-badge">{{ S.settings.owner }}</span>
                            <button v-if="owner && !isOwnerUser(m)" type="button" class="kb-btn kb-btn--ghost kb-btn--sm" :disabled="busy" @click="removeMember(m)">{{ S.common.remove }}</button>
                        </div>

                        <div v-if="owner" class="kb-field kb-suggest" style="margin-top:16px">
                            <label class="kb-field__label">{{ S.settings.addMember }}</label>
                            <input class="kb-input" v-model="memberLogin" :maxlength="LIMITS.login" :placeholder="S.settings.memberLoginPlaceholder" @input="searchMembers" />
                            <ul v-if="suggestions.length" class="kb-suggest__list">
                                <li v-for="u in suggestions" :key="u.userId" class="kb-suggest__item" @click="pickSuggestion(u)">{{ u.login }}</li>
                            </ul>
                            <div class="kb-comment-form__row">
                                <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" :disabled="busy || !memberLogin.trim()" @click="addMember">{{ S.common.add }}</button>
                            </div>
                        </div>
                    </div>

                    <div v-else-if="tab === 'about'">
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.boards.name }}</label>
                            <input class="kb-input" v-model="boardName" :maxlength="LIMITS.boardName" :disabled="!owner" />
                            <div v-if="fieldErrors.name" class="kb-field__error">{{ fieldErrors.name }}</div>
                        </div>
                        <div class="kb-field">
                            <label class="kb-field__label">{{ S.boards.description }}</label>
                            <textarea class="kb-textarea" v-model="boardDescription" :maxlength="LIMITS.description" :disabled="!owner"></textarea>
                            <div v-if="fieldErrors.description" class="kb-field__error">{{ fieldErrors.description }}</div>
                        </div>
                        <div v-if="owner" class="kb-comment-form__row">
                            <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" :disabled="busy" @click="saveBoard">{{ S.common.save }}</button>
                            <button type="button" class="kb-btn kb-btn--danger kb-btn--sm" style="margin-left:auto" :disabled="busy" @click="deleteBoard">{{ S.settings.deleteBoard }}</button>
                        </div>
                    </div>
                </div>

                <div class="kb-modal__foot">
                    <button type="button" class="kb-btn" @click="$emit('close')">{{ S.common.close }}</button>
                </div>
            </div>
        </div>
    `
};
```

- [ ] В `wwwroot/js/components/BoardView.js` добавить импорт. Найти фрагмент:
```js
import TaskPanel from '/js/components/TaskPanel.js';
```
Заменить на:
```js
import TaskPanel from '/js/components/TaskPanel.js';
import BoardSettingsModal from '/js/components/BoardSettingsModal.js';
```

- [ ] В `wwwroot/js/components/BoardView.js` зарегистрировать компонент. Найти фрагмент:
```js
    components: { TaskColumn, TaskPanel },
```
Заменить на:
```js
    components: { TaskColumn, TaskPanel, BoardSettingsModal },
```

- [ ] В `wwwroot/js/components/BoardView.js` добавить состояние модалки. Найти фрагмент:
```js
        const creating = ref(false);
        const form = ref({ taskName: '', taskDescription: '', deadlineLocal: '', workerId: '', statusId: null });
```
Заменить на:
```js
        const creating = ref(false);
        const settingsOpen = ref(false);
        const form = ref({ taskName: '', taskDescription: '', deadlineLocal: '', workerId: '', statusId: null });
```

- [ ] В `wwwroot/js/components/BoardView.js` экспортировать флаг в шаблон. Найти фрагмент:
```js
            creating, form, busy, formError, fieldErrors, openCreate, createTask, onMove, pushToast
```
Заменить на:
```js
            creating, settingsOpen, form, busy, formError, fieldErrors, openCreate, createTask, onMove, pushToast
```

- [ ] В `wwwroot/js/components/BoardView.js` подключить кнопку настроек. Найти фрагмент:
```js
                <button type="button" class="kb-btn kb-btn--sm" @click="pushToast(S.settings.title)">{{ S.board.settings }}</button>
```
Заменить на:
```js
                <button type="button" class="kb-btn kb-btn--sm" @click="settingsOpen = true">{{ S.board.settings }}</button>
```

- [ ] В `wwwroot/js/components/BoardView.js` отрисовать модалку. Найти фрагмент:
```js
            <TaskPanel v-if="store.task" :key="store.task.taskId" :board-id="boardId" :task="store.task" />
```
Заменить на:
```js
            <TaskPanel v-if="store.task" :key="store.task.taskId" :board-id="boardId" :task="store.task" />

            <BoardSettingsModal v-if="settingsOpen" @close="settingsOpen = false" />
```

- [ ] Запустить приложение:
```bash
cd . && dotnet run
```

- [ ] Проверить вкладку «Участники» у владельца: открыть доску «Фронт», нажать «Настройки». Ожидается: модалка «Настройки доски» с вкладками «Участники» и «О доске»; в списке — `pavel-front` с бейджем «владелец» и без кнопки «Удалить»; ниже поле «Добавить участника».

- [ ] Проверить автокомплит и добавление: зарегистрировать второго пользователя (в приватном окне создать `masha-front`/`123456`), вернуться в первое окно, в поле «Добавить участника» ввести `mas`. Ожидается: под полем выпадает список с `masha-front`; кликнуть по нему — логин подставился; нажать «Добавить» — участник появился в списке модалки и его аватар появился в шапке доски.

- [ ] Проверить несуществующего пользователя: ввести `nobody-xyz`, «Добавить». Ожидается: внутри модалки красная плашка «Пользователь не найден», доска за модалкой цела, тоста нет.

- [ ] Проверить read-only для не-владельца: в приватном окне войти как `masha-front`, открыть доску «Фронт», нажать «Настройки». Ожидается: текст «Изменять настройки может только владелец доски»; в списке участников нет кнопок «Удалить»; на вкладке «О доске» поля неактивны, кнопок «Сохранить» и «Удалить доску» нет.

- [ ] Проверить вкладку «О доске»: в окне владельца открыть «Настройки» → «О доске», изменить название на `Фронт v2`, «Сохранить». Ожидается: `PUT /api/boards/{id}` 200, название обновилось в шапке доски и в сайдбаре без перезагрузки.

- [ ] Проверить удаление участника: «Настройки» → «Участники» → «Удалить» у `masha-front`. Ожидается: участник исчез из списка и из шапки доски; у задач, где он был исполнителем, аватар сменился на серый `?`. Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/BoardSettingsModal.js wwwroot/js/components/BoardView.js && git commit -m "Настройки доски: вкладки участников и «О доске»"
```

---

### Task 21: Настройки доски — вкладка «Колонки» и финальная приёмка

**Files:**
- Modify: `./wwwroot/js/components/BoardSettingsModal.js` (импорты, setup, шаблон)

- [ ] В `wwwroot/js/components/BoardSettingsModal.js` расширить импорт стора. Найти фрагмент:
```js
import { store, isBoardOwner, upsertBoard, removeBoard, navigate, pushToast, loadBoard } from '/js/store.js';
```
Заменить на:
```js
import { store, isBoardOwner, upsertBoard, removeBoard, navigate, pushToast, loadBoard, applyStatusPositions, sortStatuses } from '/js/store.js';
```

- [ ] В `wwwroot/js/components/BoardSettingsModal.js` добавить логику колонок. Найти фрагмент:
```js
        // --- О доске ---
        const boardName = ref(store.board ? store.board.nameOfBoard : '');
```
Заменить на:
```js
        // --- Колонки ---
        const newColumnName = ref('');
        const editingStatusId = ref(null);
        const editingName = ref('');
        const dragStatusId = ref(null);
        const dragOverStatusId = ref(null);
        const columnsEl = ref(null);

        function columnError(e) {
            if (e.status === 404) return S.errors.modalNotFound;
            if (e.status === 409) return e.message || S.errors.conflict;
            return e.message;
        }

        async function addColumn() {
            if (busy.value) return;
            busy.value = true;
            error.value = '';
            try {
                const created = await api.createStatus(boardId.value, newColumnName.value);
                store.statuses.push(created);
                sortStatuses();
                newColumnName.value = '';
            } catch (e) {
                error.value = columnError(e);
            } finally {
                busy.value = false;
            }
        }

        function startRename(status) {
            editingStatusId.value = status.statusId;
            editingName.value = status.statusName;
        }

        async function saveRename(status) {
            if (busy.value) return;
            busy.value = true;
            error.value = '';
            try {
                const updated = await api.renameStatus(boardId.value, status.statusId, editingName.value);
                const i = store.statuses.findIndex((s) => s.statusId === status.statusId);
                if (i >= 0) store.statuses[i] = updated;
                editingStatusId.value = null;
            } catch (e) {
                error.value = columnError(e);
            } finally {
                busy.value = false;
            }
        }

        async function deleteColumn(status) {
            if (busy.value) return;
            busy.value = true;
            error.value = '';
            try {
                await api.deleteStatus(boardId.value, status.statusId);
                const i = store.statuses.findIndex((s) => s.statusId === status.statusId);
                if (i >= 0) store.statuses.splice(i, 1);
                applyStatusPositions(store.statuses.map((s, idx) => ({ id: s.statusId, order: idx })));
            } catch (e) {
                error.value = columnError(e);
            } finally {
                busy.value = false;
            }
        }

        function onColumnDragStart(status) {
            dragStatusId.value = status.statusId;
        }

        function onColumnDragOver(status, event) {
            if (dragStatusId.value === null) return;
            event.preventDefault();
            dragOverStatusId.value = status.statusId;
        }

        async function onColumnDrop(status) {
            const movedId = dragStatusId.value;
            dragStatusId.value = null;
            dragOverStatusId.value = null;
            if (!movedId || movedId === status.statusId) return;

            // Position — индекс в списке колонок ПОСЛЕ изъятия перемещаемой колонки.
            const rest = store.statuses.filter((s) => s.statusId !== movedId);
            const position = rest.findIndex((s) => s.statusId === status.statusId);
            if (position < 0) return;

            error.value = '';
            try {
                const affected = await api.moveStatus(boardId.value, movedId, position);
                applyStatusPositions(affected);
            } catch (e) {
                error.value = columnError(e);
                pushToast(S.errors.moveColumnFailed, 'error');
                await loadBoard(boardId.value);
            }
        }

        // --- О доске ---
        const boardName = ref(store.board ? store.board.nameOfBoard : '');
```

- [ ] В `wwwroot/js/components/BoardSettingsModal.js` экспортировать новое в шаблон. Найти фрагмент:
```js
            memberLogin, suggestions, searchMembers, pickSuggestion, addMember, removeMember, isOwnerUser,
```
Заменить на:
```js
            memberLogin, suggestions, searchMembers, pickSuggestion, addMember, removeMember, isOwnerUser,
            newColumnName, editingStatusId, editingName, dragStatusId, dragOverStatusId, columnsEl,
            addColumn, startRename, saveRename, deleteColumn, onColumnDragStart, onColumnDragOver, onColumnDrop,
```

- [ ] В `wwwroot/js/components/BoardSettingsModal.js` добавить вкладку в переключатель. Найти фрагмент:
```js
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'members' }" @click="tab = 'members'">{{ S.settings.tabMembers }}</button>
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'about' }" @click="tab = 'about'">{{ S.settings.tabAbout }}</button>
```
Заменить на:
```js
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'members' }" @click="tab = 'members'">{{ S.settings.tabMembers }}</button>
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'columns' }" @click="tab = 'columns'">{{ S.settings.tabColumns }}</button>
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'about' }" @click="tab = 'about'">{{ S.settings.tabAbout }}</button>
```

- [ ] В `wwwroot/js/components/BoardSettingsModal.js` добавить содержимое вкладки. Найти фрагмент:
```js
                    <div v-else-if="tab === 'about'">
```
Заменить на:
```js
                    <div v-else-if="tab === 'columns'">
                        <div class="kb-empty kb-empty--sm" v-if="owner">{{ S.settings.columnsHint }}</div>
                        <div ref="columnsEl">
                            <div v-for="s in store.statuses"
                                 :key="s.statusId"
                                 class="kb-row"
                                 :class="{ 'kb-row--drag': owner, 'is-over': dragOverStatusId === s.statusId }"
                                 :draggable="owner && editingStatusId !== s.statusId"
                                 @dragstart="onColumnDragStart(s)"
                                 @dragover="onColumnDragOver(s, $event)"
                                 @drop.prevent="onColumnDrop(s)">
                                <template v-if="editingStatusId === s.statusId">
                                    <input class="kb-input" v-model="editingName" :maxlength="LIMITS.columnName" />
                                    <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" :disabled="busy" @click="saveRename(s)">{{ S.common.save }}</button>
                                    <button type="button" class="kb-btn kb-btn--sm" @click="editingStatusId = null">{{ S.common.cancel }}</button>
                                </template>
                                <template v-else>
                                    <span class="kb-row__main">{{ s.statusName }}</span>
                                    <button v-if="owner" type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="startRename(s)">{{ S.common.edit }}</button>
                                    <button v-if="owner" type="button" class="kb-btn kb-btn--ghost kb-btn--sm" :disabled="busy" @click="deleteColumn(s)">{{ S.common.remove }}</button>
                                </template>
                            </div>
                        </div>

                        <div v-if="owner" class="kb-field" style="margin-top:16px">
                            <label class="kb-field__label">{{ S.settings.addColumn }}</label>
                            <input class="kb-input" v-model="newColumnName" :maxlength="LIMITS.columnName" :placeholder="S.settings.columnName" />
                            <div class="kb-comment-form__row">
                                <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" :disabled="busy || !newColumnName.trim()" @click="addColumn">{{ S.common.add }}</button>
                            </div>
                        </div>
                    </div>

                    <div v-else-if="tab === 'about'">
```

- [ ] Запустить приложение:
```bash
cd . && dotnet run
```

- [ ] Проверить создание колонки: открыть доску, «Настройки» → «Колонки», ввести `Ревью`, «Добавить». Ожидается: колонка появилась в списке модалки последней; закрыть модалку — колонка «Ревью» есть на доске справа.

- [ ] Проверить переименование: нажать «Редактировать» у «Ревью», ввести `На ревью`, «Сохранить». Ожидается: `PUT /api/boards/{id}/statuses/{statusId}` 200, имя обновилось и в модалке, и на доске.

- [ ] Проверить перетаскивание порядка: перетащить строку «На ревью» на первую позицию списка. Ожидается: `PATCH .../statuses/{statusId}/position` 200, порядок строк в модалке изменился; закрыть модалку — колонки на доске идут в новом порядке; после F5 порядок сохранён.

- [ ] Проверить удаление непустой колонки: перетащить любую карточку в колонку «На ревью», затем в настройках нажать у неё «Удалить». Ожидается: внутри модалки красная плашка «В колонке есть задачи» (409), колонка не удалена, доска цела.

- [ ] Проверить удаление пустой колонки: убрать карточку из «На ревью» (перетащить обратно на доске), снова нажать «Удалить». Ожидается: колонка исчезла из списка и с доски, ошибок нет.

- [ ] Проверить запрет на последнюю колонку: удалить все колонки, кроме одной, затем попытаться удалить последнюю. Ожидается: в модалке «Нельзя удалить последнюю колонку», колонка на месте.

- [ ] Проверить read-only для не-владельца: в приватном окне войти вторым пользователем (добавив его в участники доски), открыть «Настройки» → «Колонки». Ожидается: список колонок виден, кнопок «Редактировать»/«Удалить», поля добавления и подсказки о перетаскивании нет; строки не перетаскиваются.

- [ ] Финальная сквозная приёмка (§9 спеки) — одним проходом в браузере на `http://localhost:5110`: регистрация нового пользователя → создание доски → создание/переименование/перетаскивание/удаление колонки → создание задач с исполнителем и без → DnD между колонками и внутри колонки → открытие панели задачи → комментарий без файла и с файлом → вложение задачи (загрузка, скачивание, удаление) → смена статуса и проверка истории (свежие сверху) → добавление и удаление участника → удаление доски → переключение тем «Светлая»/«Тёмная»/«Системная». Ожидается: на каждом шаге ошибок в консоли браузера нет, все запросы в Network со статусами 200/201/204, интерфейс полностью на русском, в обеих темах текст читаем и рамки/фон соответствуют теме.

- [ ] Проверить, что после удаления доски приложение цело: удалить тестовую доску через «Настройки» → «О доске» → «Удалить доску» → подтвердить. Ожидается: зелёный тост «Доска удалена», редирект на `#/`, доска исчезла из сайдбара и из сетки. Остановить приложение (Ctrl+C).

- [ ] Закоммитить:
```bash
cd . && git add wwwroot/js/components/BoardSettingsModal.js && git commit -m "Настройки доски: вкладка колонок с CRUD и перетаскиванием порядка"
```
