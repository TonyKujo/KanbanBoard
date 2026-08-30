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
