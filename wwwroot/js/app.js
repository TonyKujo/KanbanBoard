// Смоук-версия точки входа: проверяем, что вендоренный Vue грузится и монтируется.
// Полная версия (роутер + компоненты) появится в Task 12.
import { createApp } from '/lib/vue.esm-browser.prod.js';

createApp({
    template: '<div class="kb-boot">Каркас SPA работает</div>'
}).mount('#app');
