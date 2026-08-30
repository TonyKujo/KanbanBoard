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
