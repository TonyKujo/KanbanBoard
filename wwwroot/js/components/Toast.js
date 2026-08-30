import { store, removeToast } from '/js/store.js';

export default {
    name: 'Toast',
    setup() {
        return { store, removeToast };
    },
    template: `
        <div class="kb-toasts">
            <div v-for="t in store.toasts"
                 :key="t.id"
                 class="kb-toast"
                 :class="'kb-toast--' + t.kind"
                 @click="removeToast(t.id)">{{ t.text }}</div>
        </div>
    `
};
