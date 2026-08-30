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
