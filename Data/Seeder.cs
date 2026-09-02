using Bogus;
using KanbanBoard.Models;
using Microsoft.EntityFrameworkCore;
using Task = KanbanBoard.Models.Task;

namespace KanbanBoard.Data
{
    public static class Seeder
    {
        public static async System.Threading.Tasks.Task SeedAsync(KanbanBoardDbContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                TRUNCATE TABLE ""TaskStatusHistories"", ""Comments"", ""Tasks"", ""Statuses"", ""BoardUsers"", ""Boards"", ""Users"" CASCADE;
            ");

            var faker = new Faker();

            // 1. Пользователи (500)
            var users = new List<User>();
            for (int i = 1; i <= 500; i++)
            {
                var firstName = faker.Name.FirstName();
                var lastName = faker.Name.LastName();
                var login = Truncate(faker.Internet.UserName(firstName, lastName), 100);
                users.Add(new User
                {
                    Login = login,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    DateOfRegistration = faker.Date.Past(2).ToUniversalTime()
                });
            }
            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            // 2. Домены
            var domains = new Dictionary<string, DomainData>();

            domains["Финансы"] = new DomainData(
                Tone.Formal,
                new[] { "Финансовый учёт", "Зарплатный проект", "Бюджетирование", "Отчётность", "Налоговый модуль" },
                new[] { "Review", "Testing", "Blocked", "On Hold", "UAT", "Financial Audit" },
                new[] { "Разработать модуль расчёта зарплат", "Исправить ошибку в отчёте по налогам", "Добавить импорт банковских выписок", "Настроить интеграцию с 1С", "Сверстать страницу бухгалтерского баланса", "Реализовать выгрузку в Excel", "Сделать автоматическую сверку счетов", "Добавить валютный контроль", "Настроить уведомления о платежах", "Подготовить отчёт по НДС", "Внедрить расчёт больничных", "Сделать API для контрагентов", "Разработать книгу покупок/продаж", "Автоматизировать расчёт командировочных", "Обновить справочник курсов валют", "Настроить согласование платежей", "Сделать выгрузку для банка", "Реализовать расчёт премий", "Добавить отчёт по дебиторке", "Интеграция с системой электронного документооборота" },
                new[] { "Необходимо реализовать функционал согласно регламенту.", "Задача из спринта, уточнить требования у аналитика.", "Исправить баг, воспроизводится на проде, критично для пользователей.", "Провести рефакторинг и оптимизацию запросов.", "Подготовить решение, согласовать с архитектором, затем реализовать.", "Сделать по образцу предыдущего отчёта, но с новыми колонками.", "Требуется добавить проверки на отрицательные значения.", "Разобраться с округлением сумм, расхождение на копейки.", "Добавить поддержку нескольких валют." },
                new[] { "Убедительно прошу проверить.", "С уважением, жду ответа.", "Прошу обратить внимание.", "Необходимо согласовать с бухгалтерией." }
            );

            domains["CRM"] = new DomainData(
                Tone.SemiFormal,
                new[] { "CRM система", "Управление клиентами", "Автоматизация продаж", "Портал для менеджеров" },
                new[] { "Lead Processing", "Opportunity Review", "Contract Signing", "Demo Scheduled", "Customer Success" },
                new[] { "Реализовать API для сделок", "Добавить фильтры в список клиентов", "Настроить email-уведомления", "Создать карточку клиента", "Импорт контактов из Excel", "Разработать воронку продаж", "Интеграция с телефонией", "Автоматическое распределение лидов", "Добавить историю взаимодействий", "Реализовать отчёты по менеджерам", "Сделать напоминания о звонках", "Настроить синхронизацию с календарём", "Добавить массовое редактирование", "Настроить поля для сегментации", "Интеграция с почтовым сервисом", "Сделать скоринг лидов", "Добавить согласование скидок", "Подключить API для обмена данными" },
                new[] { "Клиент не может сохраниться, падает ошибка 500.", "Нужно добавить поле для тегов.", "Импорт из Excel работает медленно, оптимизировать.", "Согласовать с отделом продаж интерфейс.", "Добавить возможность прикреплять файлы к сделке.", "Авторизация через LDAP.", "Сделать массовое редактирование.", "Интеграция с почтой: автоматическое создание лида из письма." },
                new[] { "Проверь, пожалуйста.", "Есть пара моментов.", "Надо бы поправить.", "Обсудим?" }
            );

            domains["Мобильное приложение"] = new DomainData(
                Tone.Informal,
                new[] { "iOS разработка", "Android разработка", "Кроссплатформенное приложение", "Бэкенд для мобильного API" },
                new[] { "Design Review", "Beta Testing", "App Store Submission", "Crash Fixing", "Performance Optimization" },
                new[] { "Сверстать экран входа", "Исправить баг с пуш-уведомлениями", "Оптимизировать загрузку изображений", "Добавить тёмную тему", "Настроить deep linking", "Реализовать offline режим", "Интеграция с картами", "Обновить SDK до последней версии", "Добавить биометрическую авторизацию", "Сделать экран онбординга", "Исправить вылеты на старых устройствах", "Добавить аналитику использования", "Разобраться с утечкой памяти", "Обновить сплэш-скрин", "Сделать поддержку iOS 18" },
                new[] { "Падает на старых устройствах.", "Нужно обновить иконки и сплэш-скрин.", "Добавить аналитику использования.", "Проверить работу на iOS 18.", "Разобраться с утечкой памяти.", "Исправить поведение при сворачивании приложения.", "Обновить зависимости, проверить совместимость." },
                new[] { "Глянь, плиз.", "Опять этот баг, ахах.", "Красавчик, давай ещё.", "Что за дичь?" }
            );

            domains["Сайт"] = new DomainData(
                Tone.Informal,
                new[] { "Корпоративный сайт", "Интернет-магазин", "Промо-страница", "Блог" },
                new[] { "SEO Optimization", "Performance Testing", "Content Review", "Accessibility Check", "Cross-browser Testing" },
                new[] { "Сверстать главную страницу", "Добавить форму обратной связи", "Исправить адаптивность меню", "Настроить SEO-теги", "Подключить аналитику", "Реализовать слайдер", "Оптимизировать скорость загрузки", "Сделать версию для слабовидящих", "Интеграция с CMS", "Добавить хлебные крошки", "Обновить шрифты по брендбуку", "Добавить микроразметку Schema.org", "Настроить редиректы", "Исправить отображение на IE", "Сделать мультиязычность" },
                new[] { "Меню наезжает на контент на мобильных.", "Нужно улучшить SEO-тексты.", "Не отображается блок на IE.", "Скорость загрузки ниже нормы.", "Не работает отправка формы, нужно проверить валидацию.", "Обновить шрифты и цвета по брендбуку.", "Добавить микроразметку Schema.org." },
                new[] { "Смотри, обновил.", "Проверил, всё ок.", "Сделай красиво.", "Шрифты огонь." }
            );

            domains["Инфраструктура"] = new DomainData(
                Tone.Formal,
                new[] { "DevOps", "Базы данных", "Облако", "Безопасность" },
                new[] { "Infrastructure Review", "Load Testing", "Security Audit", "Incident Response", "Automation" },
                new[] { "Настроить CI/CD пайплайн", "Мигрировать на PostgreSQL 17", "Внедрить логирование", "Настроить мониторинг", "Обновить Docker-образы", "Развернуть staging окружение", "Написать скрипты резервного копирования", "Внедрить централизованный сбор логов", "Настроить алерты в Grafana", "Обновить сертификаты", "Провести нагрузочное тестирование", "Автоматизировать деплой", "Настроить VPN-доступ", "Обновить Kubernetes", "Внедрить секреты в CI" },
                new[] { "Сервер упал ночью, нужно расследовать.", "Обновление зависимостей вызвало конфликт.", "Нужно увеличить лимиты по памяти.", "Перейти на managed-сервис.", "Настроить автоматическое переключение при сбое.", "Обновить ключи доступа.", "Провести нагрузочное тестирование перед релизом." },
                new[] { "Прошу проверить.", "Выполнено.", "Требуется согласование.", "Ожидаю подтверждения." }
            );

            domains["Аналитика"] = new DomainData(
                Tone.SemiFormal,
                new[] { "Дашборды", "Отчётность", "Хранилище данных", "BI-платформа" },
                new[] { "Data Validation", "Visualization Review", "Dashboard QA", "Report Sign-off" },
                new[] { "Создать дашборд по продажам", "Настроить ETL-процесс", "Добавить новые метрики", "Оптимизировать SQL-запросы", "Подготовить отчёт для руководства", "Внедрить OLAP-кубы", "Настроить автоматическую выгрузку", "Сделать дашборд для отдела маркетинга", "Добавить фильтры по датам", "Проверить корректность расчёта KPI", "Визуализировать воронку продаж", "Подключить источник данных из CRM", "Сделать отчёт по регионам" },
                new[] { "Данные за прошлый месяц не сошлись.", "Нужен новый график для отдела маркетинга.", "Запрос выполняется медленно.", "Обновить справочник регионов.", "Добавить фильтр по датам.", "Проверить корректность расчёта KPI." },
                new[] { "Сверил, всё ок.", "Дашборд обновил.", "Отчёт отправил.", "Метрики добавил." }
            );

            domains["Образование"] = new DomainData(
                Tone.SemiFormal,
                new[] { "Платформа онлайн-курсов", "Система тестирования", "Расписание занятий", "Личный кабинет студента" },
                new[] { "Content Review", "Pedagogical Check", "Beta Class", "Final Exam Review" },
                new[] { "Разработать личный кабинет студента", "Добавить загрузку домашних заданий", "Настроить вебинары", "Интеграция с платёжной системой", "Создать каталог курсов", "Реализовать тестирование с таймером", "Добавить рейтинг студентов", "Сделать API для мобильного приложения", "Добавить расписание", "Настроить уведомления о начале курса", "Сделать LMS", "Подключить видеохостинг", "Внедрить проверку домашних заданий", "Добавить сертификаты" },
                new[] { "Нужно согласовать интерфейс с преподавателями.", "Студенты жалуются на сложность навигации.", "Добавить возможность оффлайн просмотра.", "Интеграция с Zoom не работает.", "Тесты не сохраняют результаты.", "Обновить структуру курса." },
                new[] { "Обновил, смотри.", "Проверил, работает.", "Выложил новый курс.", "Расписание ок." }
            );

            domains["Игры"] = new DomainData(
                Tone.Informal,
                new[] { "Игровая механика", "Графика", "Сетевая игра", "Внутриигровые покупки" },
                new[] { "Playtesting", "Balance Review", "Optimization", "Localization" },
                new[] { "Сбалансировать уровни", "Добавить новые скины", "Исправить лаги в мультиплеере", "Реализовать систему достижений", "Интеграция с Discord", "Добавить инвентарь", "Настроить серверную часть", "Сделать систему сезонов", "Добавить новые карты", "Исправить баг с инвентарём", "Оптимизировать графику", "Сделать матчмейкинг", "Локализовать на немецкий", "Добавить античит", "Внедрить кроссплей" },
                new[] { "Игроки жалуются на баг с инвентарём.", "Нужно повысить FPS на слабых устройствах.", "Локализация не влезает в UI.", "Матчмейкинг подбирает неравные команды.", "Покупка не проходит, проблема с платёжкой." },
                new[] { "Тестируй, бро.", "Забалансил.", "Скины норм.", "Лаги поправил." }
            );

            domains["Логистика"] = new DomainData(
                Tone.Formal,
                new[] { "Управление складом", "Маршрутизация", "Доставка", "Таможня" },
                new[] { "Route Optimization", "Warehouse Check", "Carrier Onboarding", "Invoice Verification" },
                new[] { "Разработать модуль отслеживания грузов", "Интеграция с транспортными компаниями", "Сделать админку склада", "Оптимизировать загрузку транспорта", "Добавить экспорт накладных", "Настроить геокодирование адресов", "Внедрить сканирование штрихкодов", "Сделать API для курьеров", "Автоматизировать маршруты", "Подключить систему тарификации", "Сделать мобильное приложение для водителей", "Настроить уведомления о доставке" },
                new[] { "Адрес не распознаётся на карте.", "Нужно добавить экспорт накладных.", "Статусы заказов не обновляются.", "Водитель не видит маршрут.", "Складская накладная не совпадает с заказом." },
                new[] { "Исправлено.", "Прошу проверить.", "Выполнено согласно регламенту.", "Ожидаю подтверждения." }
            );

            domains["Здравоохранение"] = new DomainData(
                Tone.Formal,
                new[] { "Медицинская информационная система", "Электронные карты", "Запись к врачу", "Телемедицина" },
                new[] { "Clinical Review", "HL7 Integration", "Patient Portal QA", "Telemedicine Testing" },
                new[] { "Разработать модуль записи к врачу", "Интеграция с лабораторией", "Электронная медицинская карта", "Настроить уведомления для пациентов", "Добавить видеоконсультации", "Реализовать электронные рецепты", "Интеграция с аптеками", "Сделать историю болезни", "Подключить телемедицину", "Обеспечить соответствие GDPR", "Настроить обмен данными с LIS", "Сделать онлайн-оплату" },
                new[] { "Врачи просят добавить шаблоны осмотров.", "Нужно обеспечить соответствие GDPR.", "Сбой при обмене данными с LIS.", "Видео прерывается, нужно стабилизировать.", "Пациент не может скачать результаты анализов." },
                new[] { "Обновлено.", "Прошу проверить.", "Выполнено.", "Соответствует требованиям." }
            );

            // 3. Доски (200)
            var boards = new List<Board>();
            var boardDomainKeys = new List<string>();
            var boardTones = new List<Tone>();

            foreach (var domainKey in domains.Keys)
            {
                int boardsPerDomain = 200 / domains.Count;
                for (int i = 0; i < boardsPerDomain; i++)
                {
                    var domain = domains[domainKey];
                    var board = new Board
                    {
                        NameOfBoard = Truncate($"{domain.BoardNamePrefixes[faker.Random.Int(0, domain.BoardNamePrefixes.Length - 1)]} – Спринт {faker.Random.Int(1, 30)}", 200),
                        Description = $"Команда проекта, детали уточняются.",
                        AuthorId = faker.PickRandom(users).UserId,
                        DateOfMade = faker.Date.Past(1).ToUniversalTime()
                    };
                    boards.Add(board);
                    boardDomainKeys.Add(domainKey);
                    boardTones.Add(domain.Tone);
                }
            }
            while (boards.Count < 200)
            {
                var domainKey = faker.PickRandom(domains.Keys.ToList());
                var domain = domains[domainKey];
                var board = new Board
                {
                    NameOfBoard = Truncate($"{domain.BoardNamePrefixes[faker.Random.Int(0, domain.BoardNamePrefixes.Length - 1)]} – Спринт {faker.Random.Int(1, 30)}", 200),
                    Description = $"Команда проекта, детали уточняются.",
                    AuthorId = faker.PickRandom(users).UserId,
                    DateOfMade = faker.Date.Past(1).ToUniversalTime()
                };
                boards.Add(board);
                boardDomainKeys.Add(domainKey);
                boardTones.Add(domain.Tone);
            }
            db.Boards.AddRange(boards);
            await db.SaveChangesAsync();

            // 4. Участники
            var boardUsers = new List<BoardUser>();
            foreach (var board in boards)
            {
                boardUsers.Add(new BoardUser
                {
                    BoardId = board.BoardId,
                    UserId = board.AuthorId,
                    DateOfJoin = board.DateOfMade
                });

                int participantsCount = faker.Random.Int(2, 14);
                var pickedUsers = faker.PickRandom(users, participantsCount);
                foreach (var u in pickedUsers)
                {
                    if (u.UserId != board.AuthorId && !boardUsers.Any(bu => bu.BoardId == board.BoardId && bu.UserId == u.UserId))
                        boardUsers.Add(new BoardUser
                        {
                            BoardId = board.BoardId,
                            UserId = u.UserId,
                            DateOfJoin = faker.Date.Between(board.DateOfMade, DateTime.UtcNow)
                        });
                }
            }
            db.BoardUsers.AddRange(boardUsers);
            await db.SaveChangesAsync();

            // Обновляем описания досок фактическим числом участников
            var boardUserCounts = boardUsers.GroupBy(bu => bu.BoardId)
                                            .ToDictionary(g => g.Key, g => g.Count());
            foreach (var board in boards)
            {
                int count = boardUserCounts.ContainsKey(board.BoardId) ? boardUserCounts[board.BoardId] : 1;
                board.Description = $"Команда {count} человек. Спринт {faker.Random.Int(1, 30)}.";
            }
            await db.SaveChangesAsync();

            // 5. Статусы
            var statuses = new List<Status>();
            var defaultStatusNames = new[] { "To Do", "In Progress", "Done" };
            for (int i = 0; i < boards.Count; i++)
            {
                var board = boards[i];
                var domainKey = boardDomainKeys[i];
                var domain = domains[domainKey];

                int order = 0;
                foreach (var name in defaultStatusNames)
                {
                    statuses.Add(new Status { BoardId = board.BoardId, StatusName = Truncate(name, 100), Order = order++ });
                }

                int extraCount = faker.Random.Int(0, domain.ExtraStatusNames.Length);
                var usedStatusNames = new HashSet<string>(defaultStatusNames);
                for (int j = 0; j < extraCount; j++)
                {
                    var chosenName = faker.PickRandom(domain.ExtraStatusNames);
                    if (usedStatusNames.Add(chosenName))
                    {
                        statuses.Add(new Status { BoardId = board.BoardId, StatusName = Truncate(chosenName, 100), Order = order++ });
                    }
                }
            }
            db.Statuses.AddRange(statuses);
            await db.SaveChangesAsync();

            // 6. Задачи и история статусов
            var allBoardUsers = await db.BoardUsers.ToListAsync();
            var allStatuses = await db.Statuses.ToListAsync();
            var tasks = new List<Task>();
            var taskTypes = new List<TaskType>();
            var histories = new List<TaskStatusHistory>();
            var lastStatusChangeDates = new List<DateTime>();
            var boardEvents = new Dictionary<int, List<string>>();

            foreach (var board in boards)
            {
                boardEvents[board.BoardId] = new List<string>
                {
                    "Дедлайн релиза: " + faker.Date.Future(30).ToShortDateString(),
                    "Релиз версии " + faker.System.Semver(),
                    "Обнаружен критический баг на проде",
                    "Смена приоритетов после встречи с заказчиком",
                    "Отпуск ключевого участника",
                    "Демо для заказчика в пятницу"
                };
            }

            for (int i = 0; i < boards.Count; i++)
            {
                var board = boards[i];
                var domainKey = boardDomainKeys[i];
                var domain = domains[domainKey];
                var tone = boardTones[i];

                var boardUsersOfThisBoard = allBoardUsers.Where(bu => bu.BoardId == board.BoardId).ToList();
                var statusesOfThisBoard = allStatuses.Where(s => s.BoardId == board.BoardId).OrderBy(s => s.Order).ToList();

                var todoStatus = statusesOfThisBoard.FirstOrDefault(s => s.StatusName == "To Do") ?? statusesOfThisBoard.First();

                var usedTaskNames = new HashSet<string>();
                int tasksCount = faker.Random.Int(30, 100);
                for (int j = 0; j < tasksCount; j++)
                {
                    var author = faker.PickRandom(boardUsersOfThisBoard);
                    var assignee = faker.Random.Bool(0.7f) ? faker.PickRandom(boardUsersOfThisBoard) : null;

                    var taskType = faker.PickRandom<TaskType>(Enum.GetValues<TaskType>());

                    // Безопасный выбор текущего статуса
                    Status currentStatus;
                    var available = statusesOfThisBoard;
                    var inProgress = available.FirstOrDefault(s => s.StatusName == "In Progress");
                    var blocked = available.FirstOrDefault(s => s.StatusName == "Blocked");
                    var done = available.FirstOrDefault(s => s.StatusName == "Done");
                    var review = available.FirstOrDefault(s => s.StatusName == "Review");

                    switch (taskType)
                    {
                        case TaskType.Bug:
                            var bugCandidates = new List<Status>();
                            if (inProgress != null) bugCandidates.Add(inProgress);
                            if (blocked != null) bugCandidates.Add(blocked);
                            if (todoStatus != null) bugCandidates.Add(todoStatus);
                            currentStatus = bugCandidates.Any() ? faker.PickRandom(bugCandidates) : faker.PickRandom(available);
                            break;
                        case TaskType.Documentation:
                            var docCandidates = new List<Status>();
                            if (done != null) docCandidates.Add(done);
                            if (review != null) docCandidates.Add(review);
                            currentStatus = docCandidates.Any() ? faker.PickRandom(docCandidates) : faker.PickRandom(available);
                            break;
                        default:
                            currentStatus = faker.PickRandom(available);
                            break;
                    }

                    string baseName = faker.PickRandom(domain.TaskNames);
                    string taskName = baseName;
                    int suffix = 2;
                    while (!usedTaskNames.Add(taskName))
                    {
                        taskName = $"{baseName} (v{suffix++})";
                    }

                    DateTime deadline;
                    double deadlineRoll = faker.Random.Double();
                    if (deadlineRoll < 0.3)
                    {
                        deadline = faker.Date.Past(30).ToUniversalTime();
                    }
                    else if (deadlineRoll < 0.5)
                    {
                        deadline = DateTime.UtcNow.AddDays(faker.Random.Int(1, 3));
                    }
                    else
                    {
                        deadline = faker.Date.Future(faker.Random.Int(4, 60)).ToUniversalTime();
                    }

                    var task = new Task
                    {
                        TaskName = Truncate(taskName, 50),
                        TaskDescription = Truncate(domain.TaskDescriptions[faker.Random.Int(0, domain.TaskDescriptions.Length - 1)], 3000),
                        AssigneeId = assignee?.BoardUserId,
                        AuthorId = author.BoardUserId,
                        StatusId = currentStatus.StatusId,
                        BoardId = board.BoardId,
                        CreationDate = faker.Date.Past(1).ToUniversalTime(),
                        DeadLine = deadline,
                        Order = 0
                    };
                    tasks.Add(task);
                    taskTypes.Add(taskType);

                    var orderedStatuses = statusesOfThisBoard.OrderBy(s => s.Order).ToList();
                    int startIdx = orderedStatuses.FindIndex(s => s.StatusId == todoStatus.StatusId);
                    int endIdx = orderedStatuses.FindIndex(s => s.StatusId == currentStatus.StatusId);
                    List<Status> historyStatuses;
                    if (startIdx == -1 || endIdx == -1 || startIdx > endIdx)
                    {
                        historyStatuses = new List<Status> { todoStatus, currentStatus };
                    }
                    else
                    {
                        historyStatuses = orderedStatuses.Skip(startIdx).Take(endIdx - startIdx + 1).ToList();
                    }

                    var currentDate = task.CreationDate;
                    DateTime lastStatusDate = task.CreationDate;
                    foreach (var statusEntry in historyStatuses)
                    {
                        var changeDate = (statusEntry == historyStatuses.First()) ? task.CreationDate : faker.Date.Between(currentDate, DateTime.UtcNow);
                        histories.Add(new TaskStatusHistory
                        {
                            Task = task,
                            StatusId = statusEntry.StatusId,
                            AuthorId = faker.PickRandom(boardUsersOfThisBoard).BoardUserId,
                            ChangeDate = changeDate
                        });
                        currentDate = changeDate;
                        lastStatusDate = changeDate;
                    }
                    lastStatusChangeDates.Add(lastStatusDate);
                }
            }

            db.Tasks.AddRange(tasks);
            await db.SaveChangesAsync();

            db.TaskStatusHistories.AddRange(histories);
            await db.SaveChangesAsync();

            // 7. Комментарии с ролевой логикой и защитой от повторов
            var comments = new List<Comment>();
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var taskType = taskTypes[i];
                var minCommentDate = lastStatusChangeDates[i];

                var board = boards.First(b => b.BoardId == task.BoardId);
                var domainKey = boardDomainKeys[boards.IndexOf(board)];
                var domain = domains[domainKey];
                var tone = boardTones[boards.IndexOf(board)];

                var boardUsersOfThisBoard = allBoardUsers.Where(bu => bu.BoardId == task.BoardId).ToList();
                var authorBoardUser = boardUsersOfThisBoard.FirstOrDefault(bu => bu.BoardUserId == task.AuthorId);
                var assigneeBoardUser = task.AssigneeId.HasValue ? boardUsersOfThisBoard.FirstOrDefault(bu => bu.BoardUserId == task.AssigneeId.Value) : null;

                int commentsCount = faker.Random.Int(0, 10);
                if (commentsCount > 0 && authorBoardUser != null)
                {
                    var participants = new List<BoardUser> { authorBoardUser };
                    if (assigneeBoardUser != null) participants.Add(assigneeBoardUser);
                    participants.AddRange(faker.PickRandom(boardUsersOfThisBoard, Math.Min(2, boardUsersOfThisBoard.Count)));

                    var usedPhrases = new HashSet<string>();
                    var lastCommentDate = minCommentDate;
                    string lastUsedComment = null;

                    for (int c = 0; c < commentsCount; c++)
                    {
                        var commentator = faker.PickRandom(participants);
                        string commentText;

                        bool isAuthor = commentator.BoardUserId == task.AuthorId;
                        bool isAssignee = assigneeBoardUser != null && commentator.BoardUserId == assigneeBoardUser.BoardUserId;

                        if (c == 0)
                        {
                            commentText = GenerateInitialComment(taskType, tone, faker, commentator.User.Login);
                        }
                        else
                        {
                            var statusNow = allStatuses.First(s => s.StatusId == task.StatusId).StatusName;
                            Role role = isAuthor ? Role.Author : isAssignee ? Role.Assignee : Role.Observer;
                            commentText = GenerateRoleComment(role, tone, taskType, statusNow, faker, usedPhrases);
                        }

                        if (usedPhrases.Contains(commentText))
                        {
                            // Если фраза уже была, пробуем сгенерировать другую
                            var attempts = 0;
                            while (usedPhrases.Contains(commentText) && attempts < 10)
                            {
                                Role role = isAuthor ? Role.Author : isAssignee ? Role.Assignee : Role.Observer;
                                var statusNow = allStatuses.First(s => s.StatusId == task.StatusId).StatusName;
                                commentText = GenerateRoleComment(role, tone, taskType, statusNow, faker, usedPhrases);
                                attempts++;
                            }
                        }

                        usedPhrases.Add(commentText);
                        lastUsedComment = commentText;

                        if (faker.Random.Bool(0.1f))
                        {
                            var otherTasksInBoard = tasks.Where(t => t.BoardId == task.BoardId && t.TaskId != task.TaskId).ToList();
                            if (otherTasksInBoard.Any())
                            {
                                var otherTask = faker.PickRandom(otherTasksInBoard);
                                commentText += $" (см. задачу #{otherTask.TaskId})";
                            }
                        }
                        else if (faker.Random.Bool(0.1f) && boardEvents.ContainsKey(task.BoardId))
                        {
                            commentText += $" ({faker.PickRandom(boardEvents[task.BoardId])})";
                        }

                        if (task.DeadLine < DateTime.UtcNow && faker.Random.Bool(0.3f))
                        {
                            commentText += " Дедлайн просрочен!";
                        }

                        var commentDate = faker.Date.Between(lastCommentDate, DateTime.UtcNow);
                        comments.Add(new Comment
                        {
                            TaskId = task.TaskId,
                            AuthorId = commentator.BoardUserId,
                            Text = Truncate(commentText, 10000),
                            DateOfMade = commentDate,
                            IsEdited = faker.Random.Bool(0.2f)
                        });
                        lastCommentDate = commentDate;
                    }
                }
            }

            db.Comments.AddRange(comments);
            await db.SaveChangesAsync();

            // Пересчёт Order для задач
            var allTasks = await db.Tasks.ToListAsync();
            foreach (var status in allStatuses)
            {
                var tasksInStatus = allTasks.Where(t => t.StatusId == status.StatusId)
                                            .OrderBy(t => t.CreationDate)
                                            .ThenBy(t => t.TaskId)
                                            .ToList();
                for (int i = 0; i < tasksInStatus.Count; i++)
                    tasksInStatus[i].Order = i;
            }
            await db.SaveChangesAsync();
        }

        // Вспомогательные методы генерации комментариев по ролям
        private static string GenerateInitialComment(TaskType taskType, Tone tone, Faker faker, string authorName)
        {
            string phrase = taskType switch
            {
                TaskType.Feature => tone switch
                {
                    Tone.Informal => faker.PickRandom(new[] {
                        $"Запилил задачку, разбираюсь. ({authorName})",
                        "Требования глянул, делаю.",
                        "Начинаю, скоро будут коммиты.",
                        "Фичу эту осилим.",
                        "Принял, стартую."
                    }),
                    Tone.Formal => faker.PickRandom(new[] {
                        "Приступаю к реализации. План согласован.",
                        "Ознакомился с требованиями. Приступаю к разработке.",
                        "Начинаю работу, буду докладывать о ходе."
                    }),
                    _ => faker.PickRandom(new[] {
                        "Начинаю работу, держу в курсе.",
                        "Принял задачу, займусь.",
                        "Приступаю.",
                        "Понял, делаю."
                    })
                },
                TaskType.Bug => tone switch
                {
                    Tone.Informal => faker.PickRandom(new[] {
                        "Поймал баг, чиню.",
                        "Воспроизвёл, разбираюсь.",
                        "Что-то сломалось, уже копаю.",
                        "Бага найдена, лечу.",
                        "Ща пофикшу."
                    }),
                    Tone.Formal => faker.PickRandom(new[] {
                        "Ошибка зафиксирована, приступаю к устранению.",
                        "Провожу диагностику, о результатах сообщу.",
                        "Принято в работу, исправление в приоритете."
                    }),
                    _ => faker.PickRandom(new[] {
                        "Баг нашёл, исправляю.",
                        "Воспроизвёл проблему, работаю.",
                        "Принял, буду чинить.",
                        "Занимаюсь ошибкой."
                    })
                },
                TaskType.Refactoring => tone switch
                {
                    Tone.Informal => faker.PickRandom(new[] {
                        "Рефакторить так рефакторить.",
                        "Код причешу, не переживай.",
                        "Начинаю разгребать этот ужас.",
                        "Потихоньку навожу порядок."
                    }),
                    Tone.Formal => faker.PickRandom(new[] {
                        "Приступаю к рефакторингу. Изменения будут поэтапными.",
                        "Проведу рефакторинг с сохранением функциональности.",
                        "Начинаю работы по улучшению кода."
                    }),
                    _ => faker.PickRandom(new[] {
                        "Займусь кодом, поправлю.",
                        "Рефакторинг начинаю.",
                        "Буду улучшать.",
                        "Принял, займусь структурой."
                    })
                },
                TaskType.Documentation => tone switch
                {
                    Tone.Informal => faker.PickRandom(new[] {
                        "Доки обновлю, без паники.",
                        "Пишу документацию, ждите.",
                        "Обновлю, как дойдут руки."
                    }),
                    Tone.Formal => faker.PickRandom(new[] {
                        "Приступаю к обновлению документации.",
                        "Документация будет актуализирована.",
                        "Вношу правки в описание."
                    }),
                    _ => faker.PickRandom(new[] {
                        "Обновлю документацию.",
                        "Займусь доками.",
                        "Опишу работу.",
                        "Принял, пишу."
                    })
                },
                TaskType.Integration => tone switch
                {
                    Tone.Informal => faker.PickRandom(new[] {
                        "Интеграцию настраиваю, будет весело.",
                        "Подключаю внешний сервис.",
                        "Стыкую системы, всё пучком."
                    }),
                    Tone.Formal => faker.PickRandom(new[] {
                        "Начинаю интеграцию, этапы будут согласованы.",
                        "Приступаю к подключению внешнего сервиса.",
                        "Запускаю процесс интеграции."
                    }),
                    _ => faker.PickRandom(new[] {
                        "Интеграцией займусь.",
                        "Настраиваю интеграцию.",
                        "Подключаю API.",
                        "Принял, интегрирую."
                    })
                },
                _ => "Принял задачу."
            };
            return phrase;
        }

        private static string GenerateRoleComment(Role role, Tone tone, TaskType taskType, string statusName, Faker faker, HashSet<string> usedPhrases)
        {
            var options = new List<string>();

            // Общие фразы по статусам
            var statusOptions = new Dictionary<string, List<string>>();
            statusOptions["To Do"] = tone switch
            {
                Tone.Formal => new List<string> {
                    "Задача зарегистрирована, ожидает исполнителя.",
                    "Приступаю к выполнению в ближайшее время.",
                    "Требуется уточнение по срокам.",
                    "Прошу назначить ответственного.",
                    "Включено в план текущего спринта."
                },
                Tone.Informal => new List<string> {
                    "Возьму, как дойдут руки.",
                    "В планах на эту неделю.",
                    "Начну после созвона.",
                    "Как будет время, займусь.",
                    "Висит, но не горит."
                },
                _ => new List<string> {
                    "Посмотрю на следующей неделе.",
                    "Обсудим на стендапе.",
                    "Пока не горит, но сделаю.",
                    "Планирую начать.",
                    "Есть вопросы, уточню."
                }
            };
            statusOptions["In Progress"] = tone switch
            {
                Tone.Formal => new List<string> {
                    "Работа выполняется, промежуточные результаты положительные.",
                    "Исполнение продолжается, рисков не выявлено.",
                    "Прогресс соответствует плану.",
                    "Веду разработку, отчёт предоставлю.",
                    "Требуется дополнительное согласование."
                },
                Tone.Informal => new List<string> {
                    "Пилим потихоньку.",
                    "Делаю, не мешай ;)",
                    "Уже близко, осталось немного.",
                    "Есть нюанс, но решаемый.",
                    "Скоро покажу результат."
                },
                _ => new List<string> {
                    "Работаю, прогресс есть.",
                    "Идёт разработка, всё по плану.",
                    "Занимаюсь, скоро закончу.",
                    "Есть пара моментов, уточняю.",
                    "Продвигаюсь, завтра будет больше."
                }
            };
            statusOptions["Done"] = tone switch
            {
                Tone.Formal => new List<string> {
                    "Работа завершена, прошу проверить.",
                    "Выполнено в полном объёме.",
                    "Готово, ожидаю подтверждения.",
                    "Все требования выполнены.",
                    "Задача закрыта."
                },
                Tone.Informal => new List<string> {
                    "Готово, можно тестить.",
                    "Закрываю, все довольны?",
                    "Сделано, жду фидбек.",
                    "Всё работает, проверяйте.",
                    "Красота, готово."
                },
                _ => new List<string> {
                    "Сделано, проверяйте.",
                    "Задача закрыта.",
                    "Готово, можно смотреть.",
                    "Выполнено.",
                    "Готово."
                }
            };
            statusOptions["Blocked"] = tone switch
            {
                Tone.Formal => new List<string> {
                    "Выполнение заблокировано, требуется решение смежного отдела.",
                    "Ожидаем устранения препятствия.",
                    "Блокировка, нужна эскалация.",
                    "Приостановлено до выяснения.",
                    "Не хватает ресурсов."
                },
                Tone.Informal => new List<string> {
                    "Встало колом, ждём фикс.",
                    "Заблокировано, не по нашей вине.",
                    "Нужна помощь, сами не вывозим.",
                    "Упёрлись в стену.",
                    "Ждём разблокировки."
                },
                _ => new List<string> {
                    "Ждём пока разблокируют.",
                    "Блокировка, нужна помощь.",
                    "Приостановлено.",
                    "Есть внешняя зависимость.",
                    "Решаем проблему."
                }
            };

            if (statusOptions.ContainsKey(statusName))
            {
                options.AddRange(statusOptions[statusName]);
            }
            else
            {
                options.Add("Принято.");
                options.Add("Понял.");
                options.Add("Ок.");
                options.Add("Принял.");
            }

            // Добавляем ролевые фразы
            var roleOptions = role switch
            {
                Role.Author => new List<string> {
                    "Я автор, но могу помочь с уточнением требований.",
                    "Готов ответить на вопросы по постановке.",
                    "Слежу за прогрессом, пишите, если что-то нужно."
                },
                Role.Assignee => new List<string> {
                    "Принял, делаю.",
                    "Взял в работу, постараюсь успеть.",
                    "Уже занимаюсь, скоро будут результаты."
                },
                Role.Observer => new List<string> {
                    "Могу подсказать, если нужно.",
                    "Обратите внимание на этот момент.",
                    "Есть идея, как улучшить.",
                    "Поддерживаю, давайте двигаться."
                },
                _ => new List<string>()
            };
            options.AddRange(roleOptions);

            // Исключаем использованные
            var available = options.Where(o => !usedPhrases.Contains(o)).ToList();
            if (available.Count == 0)
            {
                available = options; // если всё использовано, разрешаем повторы
            }

            return faker.PickRandom(available);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength);
        }

        public enum Role { Author, Assignee, Observer }
    }

    public enum Tone { Formal, SemiFormal, Informal }
    public enum TaskType { Feature, Bug, Refactoring, Documentation, Integration }

    public class DomainData
    {
        public Tone Tone { get; }
        public string[] BoardNamePrefixes { get; }
        public string[] ExtraStatusNames { get; }
        public string[] TaskNames { get; }
        public string[] TaskDescriptions { get; }
        public string[] CommentTexts { get; }

        public DomainData(Tone tone, string[] boardPrefixes, string[] extraStatuses, string[] taskNames, string[] taskDescriptions, string[] commentTexts)
        {
            Tone = tone;
            BoardNamePrefixes = boardPrefixes;
            ExtraStatusNames = extraStatuses;
            TaskNames = taskNames;
            TaskDescriptions = taskDescriptions;
            CommentTexts = commentTexts;
        }
    }
}