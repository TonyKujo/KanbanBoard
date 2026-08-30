import { ref, computed } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { store, visibleTasks } from '/js/store.js';
import TaskCard from '/js/components/TaskCard.js';

export default {
    name: 'TaskColumn',
    components: { TaskCard },
    props: {
        status: { type: Object, required: true }
    },
    emits: ['move', 'create'],
    setup(props, { emit }) {
        const bodyEl = ref(null);
        const isOver = ref(false);

        const tasks = computed(() => visibleTasks(props.status.statusId));

        // Индекс вставки считаем по серединам карточек, исключая перетаскиваемую —
        // это и есть Position: индекс в целевой колонке после изъятия элемента.
        function calcIndex(event) {
            if (!bodyEl.value) return 0;
            const cards = Array.from(bodyEl.value.querySelectorAll('[data-task-id]'))
                .filter((el) => Number(el.dataset.taskId) !== store.draggingTaskId);
            for (let i = 0; i < cards.length; i++) {
                const rect = cards[i].getBoundingClientRect();
                if (event.clientY < rect.top + rect.height / 2) return i;
            }
            return cards.length;
        }

        function onDragOver(event) {
            if (store.draggingTaskId === null) return;
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
            isOver.value = true;
        }

        function onDragLeave(event) {
            if (bodyEl.value && event.relatedTarget && bodyEl.value.contains(event.relatedTarget)) return;
            isOver.value = false;
        }

        function onDrop(event) {
            event.preventDefault();
            isOver.value = false;
            const taskId = Number(event.dataTransfer.getData('text/plain'));
            if (!taskId) return;
            emit('move', { taskId, statusId: props.status.statusId, position: calcIndex(event) });
            store.draggingTaskId = null;
        }

        return { S, store, bodyEl, isOver, tasks, onDragOver, onDragLeave, onDrop, emit };
    },
    template: `
        <section class="kb-column"
                 :class="{ 'is-over': isOver }"
                 @dragover="onDragOver"
                 @dragleave="onDragLeave"
                 @drop="onDrop">
            <header class="kb-column__head">
                <span>{{ status.statusName }}</span>
                <span class="kb-column__count">{{ tasks.length }}</span>
            </header>

            <div class="kb-column__body" ref="bodyEl">
                <TaskCard v-for="t in tasks" :key="t.taskId" :task="t" />
                <div v-if="tasks.length === 0" class="kb-empty kb-empty--sm">
                    {{ store.search.trim() ? S.board.noSearchResults : S.board.emptyColumn }}
                </div>
            </div>

            <footer class="kb-column__foot">
                <button type="button" class="kb-btn kb-btn--ghost kb-btn--sm" @click="$emit('create', status.statusId)">{{ S.board.addTask }}</button>
            </footer>
        </section>
    `
};
