// Чистые хелперы отображения. Состояния здесь нет — только форматирование.
import { S } from '/js/strings.js';

// Бек отдаёт DateTime; если Kind оказался Unspecified, суффикса Z в JSON не будет.
// Считаем такие даты UTC — иначе браузер сдвинет их на локальную зону.
export function parseDate(value) {
    if (!value) return null;
    const hasZone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(value);
    const d = new Date(hasZone ? value : value + 'Z');
    return isNaN(d.getTime()) ? null : d;
}

const dateFmt = new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
const dateTimeFmt = new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
});
const shortFmt = new Intl.DateTimeFormat('ru-RU', { day: '2-digit', month: 'short' });

export function formatDate(value) {
    const d = parseDate(value);
    return d ? dateFmt.format(d) : S.common.nothing;
}

export function formatDateTime(value) {
    const d = parseDate(value);
    return d ? dateTimeFmt.format(d) : S.common.nothing;
}

// Бейдж дедлайна: просрочен — красный, ближайшие сутки — жёлтый, иначе нейтральный.
export function deadlineInfo(value) {
    const d = parseDate(value);
    if (!d) return { text: '', state: 'none' };
    const diff = d.getTime() - Date.now();
    let state = 'normal';
    if (diff < 0) state = 'overdue';
    else if (diff < 24 * 60 * 60 * 1000) state = 'soon';
    const sameYear = d.getFullYear() === new Date().getFullYear();
    return { text: sameYear ? shortFmt.format(d) : dateFmt.format(d), state };
}

export function initials(login) {
    return (login || '?').trim().charAt(0) || '?';
}

// Детерминированный цвет аватара по логину: картинок на беке нет и не будет.
export function avatarColor(login) {
    const s = login || '';
    let h = 0;
    for (let i = 0; i < s.length; i++) {
        h = (h * 31 + s.charCodeAt(i)) % 360;
    }
    return `hsl(${h}, 52%, 46%)`;
}

export function avatarStyle(login) {
    return { background: avatarColor(login) };
}

// ISO с сервера -> значение для <input type="datetime-local"> (локальное время без зоны).
export function toLocalInputValue(value) {
    const d = parseDate(value);
    if (!d) return '';
    const pad = (n) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
