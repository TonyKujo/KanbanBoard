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
