import { ref } from '/lib/vue.esm-browser.prod.js';
import { S } from '/js/strings.js';
import { api, LIMITS } from '/js/api.js';
import { store, navigate, loadBoards } from '/js/store.js';

export default {
    name: 'LoginPage',
    setup() {
        const mode = ref('login');           // 'login' | 'register'
        const login = ref('');
        const password = ref('');
        const busy = ref(false);
        const formError = ref('');
        const fieldErrors = ref({});

        function switchMode(value) {
            mode.value = value;
            formError.value = '';
            fieldErrors.value = {};
        }

        async function submit() {
            if (busy.value) return;
            busy.value = true;
            formError.value = '';
            fieldErrors.value = {};
            try {
                if (mode.value === 'login') {
                    await api.login(login.value, password.value);
                } else {
                    await api.register(login.value, password.value);
                }
                store.user = await api.me();
                await loadBoards();
                navigate('#/');
            } catch (e) {
                fieldErrors.value = e.fieldErrors || {};
                if (e.status === 401) formError.value = S.errors.badCredentials;
                else if (e.status === 409) formError.value = e.message || S.errors.loginTaken;
                else if (e.status === 400 && Object.keys(fieldErrors.value).length === 0) formError.value = e.message;
                else if (e.status !== 400) formError.value = e.message;
            } finally {
                busy.value = false;
            }
        }

        return { S, LIMITS, mode, login, password, busy, formError, fieldErrors, switchMode, submit };
    },
    template: `
        <div class="kb-auth">
            <div class="kb-auth__card">
                <h1 class="kb-auth__title">{{ S.auth.title }}</h1>
                <p class="kb-auth__subtitle">{{ S.auth.subtitle }}</p>

                <div class="kb-tabs">
                    <button type="button" class="kb-tabs__item" :class="{ 'is-active': mode === 'login' }" @click="switchMode('login')">{{ S.auth.tabLogin }}</button>
                    <button type="button" class="kb-tabs__item" :class="{ 'is-active': mode === 'register' }" @click="switchMode('register')">{{ S.auth.tabRegister }}</button>
                </div>

                <form @submit.prevent="submit">
                    <div class="kb-field">
                        <label class="kb-field__label">{{ S.auth.login }}</label>
                        <input class="kb-input" v-model="login" :maxlength="LIMITS.login" autocomplete="username" />
                        <div v-if="fieldErrors.login" class="kb-field__error">{{ fieldErrors.login }}</div>
                    </div>
                    <div class="kb-field">
                        <label class="kb-field__label">{{ S.auth.password }}</label>
                        <input class="kb-input" type="password" v-model="password" :autocomplete="mode === 'login' ? 'current-password' : 'new-password'" />
                        <div v-if="fieldErrors.password" class="kb-field__error">{{ fieldErrors.password }}</div>
                    </div>

                    <button class="kb-btn kb-btn--primary" style="width:100%" type="submit" :disabled="busy">
                        <span v-if="busy" class="kb-spinner"></span>
                        <span>{{ mode === 'login' ? S.auth.submitLogin : S.auth.submitRegister }}</span>
                    </button>

                    <div v-if="formError" class="kb-form-error">{{ formError }}</div>
                </form>
            </div>
        </div>
    `
};
