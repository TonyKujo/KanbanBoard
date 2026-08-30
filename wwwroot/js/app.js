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
