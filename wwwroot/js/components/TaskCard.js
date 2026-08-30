import { computed } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { store, navigate, isTaskMoving } from '/js/store.js';
import { deadlineInfo, initials, avatarStyle } from '/js/ui.js';

export default {
    name: 'TaskCard',
    props: {
        task: { type: Object, required: true }
    },
    setup(props) {
        const moving = computed(() => isTaskMoving(props.task.taskId));
        const deadline = computed(() => deadlineInfo(props.task.deadline));
        const worker = computed(() => props.task.worker || null);

        function open() {
            if (!store.board) return;
            navigate('#/boards/' + store.board.boardId + '/tasks/' + props.task.taskId);
        }

        function onDragStart(event) {
            if (moving.value) {
                event.preventDefault();
                return;
            }
            event.dataTransfer.effectAllowed = 'move';
            event.dataTransfer.setData('text/plain', String(props.task.taskId));
            store.draggingTaskId = props.task.taskId;
        }

        function onDragEnd() {
            store.draggingTaskId = null;
        }

        return { S, moving, deadline, worker, initials, avatarStyle, open, onDragStart, onDragEnd };
    },
    template: `
        <div class="kb-card"
             :class="{ 'is-moving': moving }"
             :data-task-id="task.taskId"
             :draggable="!moving"
             @click="open"
             @dragstart="onDragStart"
             @dragend="onDragEnd">
            <div class="kb-card__name">{{ task.taskName }}</div>
            <div class="kb-card__foot">
                <span class="kb-badge"
                      :class="{ 'kb-badge--overdue': deadline.state === 'overdue', 'kb-badge--soon': deadline.state === 'soon' }"
                      :title="S.task.deadline">{{ deadline.text }}</span>

                <span v-if="worker" class="kb-avatar" :style="avatarStyle(worker.login)" :title="worker.login">{{ initials(worker.login) }}</span>
                <span v-else class="kb-avatar kb-avatar--empty" :title="S.task.noWorker">?</span>

                <span class="kb-card__counters">
                    <span v-if="task.commentsCount">💬 {{ task.commentsCount }}</span>
                    <span v-if="task.attachmentsCount">📎 {{ task.attachmentsCount }}</span>
                </span>
            </div>
        </div>
    `
};
