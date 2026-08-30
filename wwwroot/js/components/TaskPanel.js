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
