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
