// ── Notification sound ──────────────────────────────────────────────────
let _audioCtx = null;

// Unlock AudioContext on first user gesture (browser requirement)
function _unlockAudio() {
    if (_audioCtx) { _audioCtx.resume(); return; }
    try { _audioCtx = new (window.AudioContext || window.webkitAudioContext)(); } catch (_) {}
}
document.addEventListener('click', _unlockAudio, { once: false, passive: true });
document.addEventListener('keydown', _unlockAudio, { once: false, passive: true });

async function playNotifSound() {
    if (localStorage.getItem('notifMuted') === '1') return;
    try {
        if (!_audioCtx) _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        if (_audioCtx.state === 'suspended') await _audioCtx.resume();
        const osc = _audioCtx.createOscillator();
        const gain = _audioCtx.createGain();
        osc.connect(gain);
        gain.connect(_audioCtx.destination);
        osc.type = 'sine';
        osc.frequency.setValueAtTime(880, _audioCtx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(440, _audioCtx.currentTime + 0.15);
        gain.gain.setValueAtTime(0.18, _audioCtx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, _audioCtx.currentTime + 0.3);
        osc.start(_audioCtx.currentTime);
        osc.stop(_audioCtx.currentTime + 0.3);
    } catch (_) {}
}

let _currentToken = document.querySelector('meta[name="jwt"]')?.content;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", { accessTokenFactory: () => _currentToken })
    .withAutomaticReconnect()
    .build();

// Proactive token refresh — every 13 min (JWT expires at 15 min)
setInterval(async () => {
    try {
        const resp = await fetch('/api/auth/refresh', { method: 'POST', credentials: 'same-origin' });
        if (resp.ok) {
            const data = await resp.json();
            _currentToken = data.token;
            const meta = document.querySelector('meta[name="jwt"]');
            if (meta) meta.content = data.token;
        } else if (resp.status === 401) {
            window.location.href = '/Auth/Login';
        }
    } catch (_) {}
}, 13 * 60 * 1000);

function scrollToBottom() {
    const container = document.getElementById("messages");
    if (container) container.scrollTop = container.scrollHeight;
}

function createMessageEl(author, content, time, isMine = false, id = null) {
    const div = document.createElement("div");
    div.className = "message" + (isMine ? " mine" : "");
    if (id) div.dataset.id = id;

    const authorEl = document.createElement("div");
    authorEl.className = "message-author";
    authorEl.textContent = author;

    const contentEl = document.createElement("div");
    contentEl.className = "message-content";
    contentEl.textContent = content;

    const timeEl = document.createElement("div");
    timeEl.className = "message-time";
    timeEl.textContent = time;

    div.appendChild(authorEl);
    div.appendChild(contentEl);
    div.appendChild(timeEl);

    if (id && typeof canDeleteMessages !== 'undefined' && canDeleteMessages) {
        const btn = document.createElement("button");
        btn.className = "icon-btn msg-delete-btn";
        btn.dataset.msgId = id;
        btn.title = "Delete message";
        btn.textContent = "🗑";
        div.appendChild(btn);
    }

    return div;
}

function formatTime(dateStr) {
    return new Date(dateStr).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function getDateLabel(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString([], { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
}

function getDateKey(dateStr) {
    const d = new Date(dateStr);
    return `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
}

function ensureDateSeparator(dateStr) {
    const key = getDateKey(dateStr);
    const container = document.getElementById("messages");
    if (!container) return;
    const last = container.querySelector('.date-separator:last-of-type');
    if (last && last.dataset.dateKey === key) return;
    const sep = document.createElement('div');
    sep.className = 'date-separator';
    sep.dataset.dateKey = key;
    sep.innerHTML = `<span>${getDateLabel(dateStr)}</span>`;
    container.appendChild(sep);
}

connection.on("ReceiveChannelMessage", (msg) => {
    ensureDateSeparator(msg.sentAt);
    const el = createMessageEl(msg.authorUsername, msg.content, formatTime(msg.sentAt), false, msg.id);
    document.getElementById("messages")?.appendChild(el);
    if (typeof window.applyEmojiRendering === 'function') window.applyEmojiRendering();
    scrollToBottom();
});

connection.on("ReceiveDirectMessage", (msg) => {
    const isMine = typeof currentUserId !== 'undefined' && msg.senderId === currentUserId;
    const author = isMine ? "You" : (typeof recipientName !== 'undefined' ? recipientName : "Them");
    const el = createMessageEl(author, msg.content, formatTime(msg.sentAt), isMine);
    document.getElementById("messages")?.appendChild(el);
    scrollToBottom();
    if (!isMine) playNotifSound();
});

connection.on("ReceiveNotification", (notif) => {
    playNotifSound();

    // Update server badge if serverId is present
    if (notif.serverId) {
        const serverIcon = document.querySelector(`[data-server-id="${notif.serverId}"]`);
        if (serverIcon) {
            let badge = serverIcon.querySelector('.notif-badge');
            if (badge) {
                const current = parseInt(badge.textContent) || 0;
                badge.textContent = current >= 9 ? '9+' : (current + 1).toString();
            } else {
                badge = document.createElement('span');
                badge.className = 'notif-badge';
                badge.textContent = '1';
                serverIcon.appendChild(badge);
            }
        }
    }

    // Also update home icon badge count
    const homeIcon = document.querySelector('.home-icon');
    if (homeIcon) {
        let badge = homeIcon.querySelector('.notif-badge');
        if (badge) {
            const current = parseInt(badge.textContent) || 0;
            badge.textContent = current >= 9 ? '9+' : (current + 1).toString();
        } else {
            badge = document.createElement('span');
            badge.className = 'notif-badge';
            badge.textContent = '1';
            homeIcon.appendChild(badge);
        }
        badge.classList.add('notif-pulse');
        setTimeout(() => badge.classList.remove('notif-pulse'), 600);
    }
});

connection.on("MessageDeleted", (messageId) => {
    const el = document.querySelector(`[data-id="${messageId}"]`);
    if (el) {
        el.querySelector(".message-content").textContent = "[message deleted]";
        el.classList.add("deleted");
        el.querySelector(".msg-delete-btn")?.closest(".msg-delete-form")?.remove();
    }
});

connection.on("UserTyping", (userId) => {
    const el = document.getElementById("typing-indicator");
    if (el) el.textContent = "someone is typing...";
    setTimeout(() => { if (el) el.textContent = ''; }, 2000);
});

(async () => {
    await connection.start();
    if (typeof channelId !== 'undefined')
        await connection.invoke('JoinChannel', serverId, channelId);
    scrollToBottom();
})();

document.getElementById("message-form")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const input = document.getElementById("message-input");
    if (!input.value.trim()) return;
    await connection.invoke('SendChannelMessage', channelId, input.value);
    input.value = '';
});

document.getElementById("dm-form")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const input = document.getElementById("dm-input");
    if (!input.value.trim()) return;
    await connection.invoke('SendDirectMessage', recipientId, input.value);
    input.value = '';
});

document.getElementById("messages")?.addEventListener("click", async (e) => {
    const btn = e.target.closest(".msg-delete-btn");
    if (!btn) return;
    const messageId = btn.dataset.msgId;
    if (!messageId) return;
    await connection.invoke('DeleteChannelMessage', serverId, channelId, messageId);
});
