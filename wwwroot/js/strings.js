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
        userNotFound: 'Пользователь не найден',
        selfAlreadyMember: 'Вы уже добавлены в эту доску',
        memberAlreadyAdded: 'Пользователь уже добавлен в доску'
    }
};
