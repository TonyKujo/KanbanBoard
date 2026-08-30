import { ref } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';

const THEME_KEY = 'kb.theme';

function readTheme() {
    try {
        const value = localStorage.getItem(THEME_KEY);
        return value === 'dark' || value === 'light' ? value : 'system';
    } catch (e) {
        return 'system';
    }
}

export default {
    name: 'ThemeToggle',
    setup() {
        const mode = ref(readTheme());

        // При system атрибут не ставим — работает @media prefers-color-scheme из tokens.css.
        function setMode(value) {
            mode.value = value;
            if (value === 'light' || value === 'dark') {
                document.documentElement.setAttribute('data-theme', value);
            } else {
                document.documentElement.removeAttribute('data-theme');
            }
            try {
                localStorage.setItem(THEME_KEY, value);
            } catch (e) {
                console.error('[theme] localStorage недоступен', e);
            }
        }

        return { S, mode, setMode };
    },
    template: `
        <div class="kb-seg" :title="S.theme.label">
            <button type="button" class="kb-seg__btn" :class="{ 'is-active': mode === 'system' }" @click="setMode('system')">{{ S.theme.system }}</button>
            <button type="button" class="kb-seg__btn" :class="{ 'is-active': mode === 'light' }" @click="setMode('light')">{{ S.theme.light }}</button>
            <button type="button" class="kb-seg__btn" :class="{ 'is-active': mode === 'dark' }" @click="setMode('dark')">{{ S.theme.dark }}</button>
        </div>
    `
};
