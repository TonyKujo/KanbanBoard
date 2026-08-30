import { ref, watch } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, pushToast, bumpCounter } from '/js/store.js';
import { formatDateTime, initials, avatarStyle } from '/js/ui.js';

export default {
    name: 'CommentList',
    props: {
        boardId: { type: Number, required: true },
        taskId: { type: Number, required: true }
    },
    setup(props) {
        const comments = ref([]);
        const loading = ref(false);
        const text = ref('');
        const file = ref(null);
        const fileInput = ref(null);
        const sending = ref(false);
        const error = ref('');
        const editingId = ref(null);
        const editingText = ref('');

        let tempSeq = 0;

        async function load() {
            loading.value = true;
            error.value = '';
            try {
                comments.value = await api.comments(props.boardId, props.taskId);
            } catch (e) {
                comments.value = [];
                if (e.status !== 401) error.value = e.message;
            } finally {
                loading.value = false;
            }
        }

        watch(() => props.taskId, load, { immediate: true });

        function onFileChange(event) {
            file.value = event.target.files && event.target.files[0] ? event.target.files[0] : null;
        }

        function clearFile() {
            file.value = null;
            if (fileInput.value) fileInput.value.value = '';
        }

        async function send() {
            if (sending.value || !text.value.trim()) return;
            error.value = '';
            const body = text.value;
            const attached = file.value;

            // Чисто текстовый коммент — оптимистично с tempId.
            // Коммент с вложением — обычный спиннер: файлу нужен серверный id комментария.
            if (!attached) {
                const tempId = 'temp-' + (++tempSeq);
                const optimistic = {
                    commentId: tempId,
                    taskId: props.taskId,
                    text: body,
                    madeDate: new Date().toISOString(),
                    isEdited: false,
                    author: store.user ? { userId: store.user.userId, login: store.user.login } : { userId: 0, login: '' },
                    attachments: [],
                    pending: true
                };
                comments.value.push(optimistic);
                text.value = '';
                try {
                    const created = await api.createComment(props.boardId, props.taskId, body);
                    const i = comments.value.findIndex((c) => c.commentId === tempId);
                    if (i >= 0) comments.value[i] = created;
                    bumpCounter(props.taskId, 'commentsCount', 1);
                } catch (e) {
                    const i = comments.value.findIndex((c) => c.commentId === tempId);
                    if (i >= 0) comments.value.splice(i, 1);
                    text.value = body;
                    if (e.status !== 401) pushToast(e.message || S.errors.commentFailed, 'error');
                }
                return;
            }

            sending.value = true;
            try {
                const created = await api.createComment(props.boardId, props.taskId, body);
                try {
                    await api.uploadCommentAttachment(props.boardId, created.commentId, attached);
                } catch (e) {
                    if (e.status !== 401) pushToast(S.errors.uploadFailed, 'error');
                }
                text.value = '';
                clearFile();
                bumpCounter(props.taskId, 'commentsCount', 1);
                await load();
            } catch (e) {
                if (e.status !== 401) pushToast(e.message || S.errors.commentFailed, 'error');
            } finally {
                sending.value = false;
            }
        }

        function startEdit(comment) {
            editingId.value = comment.commentId;
            editingText.value = comment.text;
        }

        async function saveEdit(comment) {
            try {
                const updated = await api.updateComment(props.boardId, props.taskId, comment.commentId, editingText.value);
                const i = comments.value.findIndex((c) => c.commentId === comment.commentId);
                if (i >= 0) comments.value[i] = updated;
                editingId.value = null;
            } catch (e) {
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        async function remove(comment) {
            if (!confirm(S.comments.deleteConfirm)) return;
            try {
                await api.deleteComment(props.boardId, props.taskId, comment.commentId);
                const i = comments.value.findIndex((c) => c.commentId === comment.commentId);
                if (i >= 0) comments.value.splice(i, 1);
                bumpCounter(props.taskId, 'commentsCount', -1);
            } catch (e) {
                if (e.status !== 401) pushToast(e.message, 'error');
            }
        }

        function isMine(comment) {
            return !!(store.user && comment.author && comment.author.userId === store.user.userId);
        }

        return {
            S, LIMITS, api, comments, loading, text, file, fileInput, sending, error,
            editingId, editingText, formatDateTime, initials, avatarStyle,
            onFileChange, clearFile, send, startEdit, saveEdit, remove, isMine
        };
    },
    template: `
        <div>
            <div class="kb-section__title">{{ S.comments.title }}</div>

            <div v-if="loading" class="kb-empty kb-empty--sm">{{ S.common.loading }}</div>
            <div v-else-if="error" class="kb-form-error">{{ error }}</div>
            <div v-else-if="comments.length === 0" class="kb-empty kb-empty--sm">{{ S.comments.empty }}</div>

            <div v-for="c in comments" :key="c.commentId" class="kb-comment" :class="{ 'is-pending': c.pending }">
                <span class="kb-avatar" :style="avatarStyle(c.author && c.author.login)">{{ initials(c.author && c.author.login) }}</span>
                <div class="kb-comment__body">
                    <div class="kb-comment__head">
                        <span class="kb-comment__author">{{ c.author && c.author.login }}</span>
                        <span class="kb-comment__date">{{ formatDateTime(c.madeDate) }}</span>
                        <span v-if="c.isEdited" class="kb-comment__date">({{ S.comments.edited }})</span>
                        <span v-if="isMine(c) && !c.pending" class="kb-comment__actions">
                            <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="startEdit(c)">{{ S.common.edit }}</button>
                            <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="remove(c)">{{ S.common.remove }}</button>
                        </span>
                    </div>

                    <div v-if="editingId === c.commentId">
                        <textarea class="kb-textarea" v-model="editingText" :maxlength="LIMITS.comment"></textarea>
                        <div class="kb-comment-form__row">
                            <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" @click="saveEdit(c)">{{ S.common.save }}</button>
                            <button type="button" class="kb-btn kb-btn--sm" @click="editingId = null">{{ S.common.cancel }}</button>
                        </div>
                    </div>
                    <div v-else class="kb-comment__text">{{ c.text }}</div>

                    <ul class="kb-files" v-if="c.attachments && c.attachments.length">
                        <li class="kb-file" v-for="a in c.attachments" :key="a.attachmentId">
                            <span>📎</span>
                            <a :href="api.downloadUrl(boardId, a.attachmentId)">{{ a.fileName }}</a>
                        </li>
                    </ul>
                </div>
            </div>

            <div class="kb-comment-form">
                <textarea class="kb-textarea" v-model="text" :maxlength="LIMITS.comment" :placeholder="S.comments.placeholder"></textarea>
                <div class="kb-comment-form__row">
                    <input type="file" ref="fileInput" @change="onFileChange" />
                    <button type="button" class="kb-btn kb-btn--primary kb-btn--sm" :disabled="sending || !text.trim()" @click="send">
                        <span v-if="sending" class="kb-spinner"></span>
                        <span>{{ S.comments.submit }}</span>
                    </button>
                </div>
            </div>
        </div>
    `
};
