import { ref, computed } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, isBoardOwner, upsertBoard, removeBoard, navigate, pushToast, loadBoard, applyStatusPositions, sortStatuses } from '/js/store.js';
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
            newColumnName, editingStatusId, editingName, dragStatusId, dragOverStatusId, columnsEl,
            addColumn, startRename, saveRename, deleteColumn, onColumnDragStart, onColumnDragOver, onColumnDrop,
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
                        <button type="button" class="kb-tabs__item" :class="{ 'is-active': tab === 'columns' }" @click="tab = 'columns'">{{ S.settings.tabColumns }}</button>
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
