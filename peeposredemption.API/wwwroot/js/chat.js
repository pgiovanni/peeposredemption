// ── Notification sound ──────────────────────────────────────────────────
let _audioCtx = null;

// Unlock AudioContext on first user gesture (browser requirement)
function _unlockAudio() {
    if (_audioCtx) { _audioCtx.resume(); return; }
    try { _audioCtx = new (window.AudioContext || window.webkitAudioContext)(); } catch (_) {}
}
document.addEventListener('click', _unlockAudio, { once: false, passive: true });
document.addEventListener('keydown', _unlockAudio, { once: false, passive: true });

// Scroll to bottom immediately when page loads (messages are server-rendered)
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById("messages");
    if (container) container.scrollTop = container.scrollHeight;
});

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
    if (container) {
        container.scrollTop = container.scrollHeight;
        requestAnimationFrame(() => container.scrollTop = container.scrollHeight);
    }
}

function shouldGroupMessage(authorId, sentAt) {
    const container = document.getElementById("messages");
    if (!container) return false;
    const msgs = container.querySelectorAll('.message');
    if (!msgs.length) return false;
    const last = msgs[msgs.length - 1];
    if (!last.dataset.authorId || last.dataset.authorId !== authorId) return false;
    const lastTime = last.dataset.sent;
    if (!lastTime) return false;
    return (new Date(sentAt) - new Date(lastTime)) < 5 * 60 * 1000;
}

function createMessageEl(author, content, time, isMine = false, id = null, authorId = null, authorAvatarUrl = null, sentAt = null, replyTo = null) {
    const grouped = authorId && sentAt && shouldGroupMessage(authorId, sentAt);
    const div = document.createElement("div");
    div.className = "message" + (isMine ? " mine" : "") + (grouped ? " message-grouped" : "");
    if (id) div.dataset.id = id;
    if (authorId) { div.dataset.authorId = authorId; div.dataset.authorName = author; }
    if (sentAt) div.dataset.sent = sentAt;
    if (grouped && sentAt) div.dataset.groupedTime = new Date(sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    // Avatar
    const avatarLink = document.createElement(authorId ? 'a' : 'div');
    avatarLink.className = 'message-avatar-link';
    if (authorId) avatarLink.href = `/App/Profile?userId=${authorId}`;
    if (authorAvatarUrl) {
        const img = document.createElement('img');
        img.src = authorAvatarUrl;
        img.alt = author;
        img.className = 'message-avatar';
        avatarLink.appendChild(img);
    } else {
        const fb = document.createElement('div');
        fb.className = 'message-avatar message-avatar-fallback';
        fb.textContent = (author || '?')[0].toUpperCase();
        avatarLink.appendChild(fb);
    }
    div.appendChild(avatarLink);

    const authorEl = document.createElement("div");
    authorEl.className = "message-author";
    if (authorId) {
        const authorLink = document.createElement('a');
        authorLink.className = 'author-link';
        authorLink.href = `/App/Profile?userId=${authorId}`;
        authorLink.textContent = author;
        authorEl.appendChild(authorLink);
    } else {
        authorEl.textContent = author;
    }

    // Reply reference block (above content)
    if (replyTo && replyTo.messageId) {
        const ref = document.createElement('div');
        ref.className = 'message-reply-ref';
        ref.innerHTML = `<span class="reply-ref-author">@${esc(replyTo.authorUsername || 'Unknown')}</span><span class="reply-ref-preview">${esc(replyTo.contentPreview || '')}</span>`;
        ref.addEventListener('click', () => {
            const target = document.querySelector(`[data-id="${replyTo.messageId}"]`);
            if (target) {
                target.scrollIntoView({ behavior: 'smooth', block: 'center' });
                target.classList.add('message-highlight');
                setTimeout(() => target.classList.remove('message-highlight'), 1500);
            }
        });
        div.appendChild(ref);
    }

    const contentEl = document.createElement("div");
    contentEl.className = "message-content";
    contentEl.textContent = content;

    // Highlight @mentions in message content
    highlightMentions(contentEl);

    // Check if this message mentions the current user
    if (typeof currentUsername !== 'undefined' && currentUsername) {
        const lower = content.toLowerCase();
        const mentionPattern = '@' + currentUsername.toLowerCase();
        if (lower.includes(mentionPattern) || lower.includes('@everyone')) {
            div.classList.add('message-mentioned');
        }
    }

    const timeEl = document.createElement("span");
    timeEl.className = "message-time";
    timeEl.textContent = ' \u2014 ' + time;
    authorEl.appendChild(document.createTextNode(' '));
    authorEl.appendChild(timeEl);

    div.appendChild(authorEl);
    div.appendChild(contentEl);

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
    return `${d.getFullYear()}-${d.getMonth() + 1}-${d.getDate()}`;
}

function highlightMentions(el) {
    const text = el.textContent;
    if (!/@\w/.test(text)) return;
    const escaped = text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    el.innerHTML = escaped.replace(/@(\w+)/g, (match, username) => {
        const member = (typeof memberData !== 'undefined' && Array.isArray(memberData))
            ? memberData.find(m => m.Username?.toLowerCase() === username.toLowerCase())
            : null;
        if (member) {
            return `<a class="mention-text" href="/App/Profile?userId=${member.Id}">@${username}</a>`;
        }
        return `<span class="mention-text">@${username}</span>`;
    });
}

function highlightServerRenderedMentions() {
    if (typeof currentUsername === 'undefined' || !currentUsername) return;
    const container = document.getElementById('messages');
    if (!container) return;
    container.querySelectorAll('.message').forEach(msg => {
        const content = msg.querySelector('.message-content');
        if (!content) return;
        const text = content.textContent.toLowerCase();
        const mentionPattern = '@' + currentUsername.toLowerCase();
        if (text.includes(mentionPattern) || text.includes('@everyone')) {
            msg.classList.add('message-mentioned');
        }
        highlightMentions(content);
    });
}

function ensureDateSeparator(dateStr) {
    const key = getDateKey(dateStr);
    const container = document.getElementById("messages");
    if (!container) return;
    const separators = container.querySelectorAll('.date-separator');
    const last = separators.length ? separators[separators.length - 1] : null;
    if (last && last.dataset.dateKey === key) return;
    const sep = document.createElement('div');
    sep.className = 'date-separator';
    sep.dataset.dateKey = key;
    sep.innerHTML = `<span>${getDateLabel(dateStr)}</span>`;
    container.appendChild(sep);
}

connection.on("ReceiveChannelMessage", (msg) => {
    ensureDateSeparator(msg.sentAt);
    const replyTo = msg.replyToMessageId ? {
        messageId: msg.replyToMessageId,
        authorUsername: msg.replyToAuthorUsername,
        contentPreview: msg.replyToContentPreview
    } : null;
    const el = createMessageEl(msg.authorUsername, msg.content, formatTime(msg.sentAt), false, msg.id, msg.authorId, msg.authorAvatarUrl, msg.sentAt, replyTo);
    document.getElementById("messages")?.appendChild(el);
    if (typeof window.applyEmojiRendering === 'function') window.applyEmojiRendering();
    scrollToBottom();

    // Play sound on @mention or @everyone (not for own messages)
    const notMyMessage = typeof currentUserId === 'undefined' || msg.authorId !== currentUserId;
    if (notMyMessage) {
        const lower = msg.content.toLowerCase();
        const mentionPattern = (typeof currentUsername !== 'undefined' && currentUsername)
            ? '@' + currentUsername.toLowerCase() : null;
        if (lower.includes('@everyone') || (mentionPattern && lower.includes(mentionPattern))) {
            playNotifSound();
        }
    }
});

connection.on("ReceiveDirectMessage", (msg) => {
    const isMine = typeof currentUserId !== 'undefined' && msg.senderId === currentUserId;
    const author = isMine ? "You" : (typeof recipientName !== 'undefined' ? recipientName : "Them");
    const avatarUrl = isMine
        ? (typeof currentUserAvatarUrl !== 'undefined' ? currentUserAvatarUrl : null)
        : (typeof recipientAvatarUrl !== 'undefined' ? recipientAvatarUrl : null);
    const el = createMessageEl(author, msg.content, formatTime(msg.sentAt), isMine, null, isMine ? currentUserId : msg.senderId, avatarUrl || null, msg.sentAt);
    document.getElementById("messages")?.appendChild(el);
    if (typeof window.applyEmojiRendering === 'function') window.applyEmojiRendering();
    scrollToBottom();
    if (!isMine) {
        playNotifSound();
        // Show DM indicator in server strip
        const container = document.getElementById('dm-indicators');
        if (container && msg.senderId && msg.senderUsername) {
            const existingId = `dm-ind-${msg.senderId}`;
            let indicator = document.getElementById(existingId);
            if (!indicator) {
                indicator = document.createElement('a');
                indicator.id = existingId;
                indicator.href = `/App/Index?friendId=${msg.senderId}`;
                indicator.className = 'server-icon dm-indicator';
                indicator.title = msg.senderUsername;
                indicator.innerHTML = `<span>${msg.senderUsername.substring(0, 1).toUpperCase()}</span><span class="notif-badge">1</span>`;
                container.appendChild(indicator);
            } else {
                // Increment badge
                const badge = indicator.querySelector('.notif-badge');
                if (badge) {
                    const current = parseInt(badge.textContent) || 0;
                    badge.textContent = current >= 9 ? '9+' : (current + 1).toString();
                }
                indicator.classList.add('notif-pulse');
                setTimeout(() => indicator.classList.remove('notif-pulse'), 600);
            }
        }
    }
});

connection.on("ReceiveNotification", (notif) => {
    // Only play sound if the notification is for the server we're currently viewing
    const isCurrentServer = typeof serverId !== 'undefined' && notif.serverId === serverId;
    if (isCurrentServer) playNotifSound();

    // Always update the server icon badge so it's visible in the strip
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

    // Notify the dropdown to refresh
    if (typeof window._notifDropdownOnNotification === 'function') window._notifDropdownOnNotification();
});

connection.on("MessageDeleted", (messageId) => {
    const el = document.querySelector(`[data-id="${messageId}"]`);
    if (el) {
        el.querySelector(".message-content").textContent = "[message deleted]";
        el.classList.add("deleted");
        el.querySelector(".msg-delete-btn")?.closest(".msg-delete-form")?.remove();
    }
});

// ── Presence: online/offline updates ────────────────────────────────────
connection.on("UserOnline", (userId) => {
    // Update DM sidebar dot
    const dmItem = document.querySelector(`.dm-item[data-user-id="${userId}"]`);
    if (dmItem) {
        const dot = dmItem.querySelector('.presence-dot');
        if (dot) { dot.classList.remove('offline'); dot.classList.add('online'); }
    }
});

connection.on("UserOffline", (userId) => {
    const dmItem = document.querySelector(`.dm-item[data-user-id="${userId}"]`);
    if (dmItem) {
        const dot = dmItem.querySelector('.presence-dot');
        if (dot) { dot.classList.remove('online'); dot.classList.add('offline'); }
    }
});

connection.on("ServerMemberOnline", (userId) => {
    const sidebar = document.getElementById('members-sidebar');
    if (!sidebar) return;
    const member = sidebar.querySelector(`[data-member-id="${userId}"]`);
    if (!member) return;

    // Move to online section
    member.classList.remove('member-offline');
    const dot = member.querySelector('.presence-dot');
    if (dot) { dot.classList.remove('offline'); dot.classList.add('online'); }

    // Re-position: insert before the offline header
    const offlineHeader = document.getElementById('offline-count');
    if (offlineHeader) sidebar.insertBefore(member, offlineHeader);

    updateMemberCounts(sidebar);
});

connection.on("ServerMemberOffline", (userId) => {
    const sidebar = document.getElementById('members-sidebar');
    if (!sidebar) return;
    const member = sidebar.querySelector(`[data-member-id="${userId}"]`);
    if (!member) return;

    member.classList.add('member-offline');
    const dot = member.querySelector('.presence-dot');
    if (dot) { dot.classList.remove('online'); dot.classList.add('offline'); }

    // Move to end (after offline header)
    sidebar.appendChild(member);

    updateMemberCounts(sidebar);
});

function updateMemberCounts(sidebar) {
    const onlineCount = sidebar.querySelectorAll('.member-item:not(.member-offline)').length;
    const offlineCount = sidebar.querySelectorAll('.member-item.member-offline').length;
    const onlineHeader = document.getElementById('online-count');
    const offlineHeader = document.getElementById('offline-count');
    if (onlineHeader) onlineHeader.textContent = `ONLINE — ${onlineCount}`;
    if (offlineHeader) offlineHeader.textContent = `OFFLINE — ${offlineCount}`;
}

connection.on("UserTyping", (userId) => {
    const el = document.getElementById("typing-indicator");
    if (el) el.textContent = "someone is typing...";
    setTimeout(() => { if (el) el.textContent = ''; }, 2000);
});

// Voice channel sidebar participant updates
connection.on("VoiceChannelState", (data) => {
    const container = document.querySelector(`[data-voice-channel="${data.channelId}"]`);
    if (!container) return;
    container.innerHTML = '';
    if (data.participants && data.participants.length > 0) {
        data.participants.forEach(p => {
            const el = document.createElement('div');
            el.className = 'voice-sidebar-user';
            const initial = (p.displayName || '?')[0].toUpperCase();
            el.innerHTML = p.avatarUrl
                ? `<img src="${p.avatarUrl}" class="voice-sidebar-avatar" alt="" /><span>${esc(p.displayName)}</span>`
                : `<div class="voice-sidebar-avatar voice-sidebar-avatar-fallback">${initial}</div><span>${esc(p.displayName)}</span>`;
            container.appendChild(el);
        });
    }
});

(async () => {
    await connection.start();
    if (typeof channelId !== 'undefined')
        await connection.invoke('JoinChannel', serverId, channelId);
    scrollToBottom();
})();

// ── Reply state ────────────────────────────────────────────────────
let _replyState = null; // { messageId, authorName, preview }

function setReplyState(messageId, authorName, preview) {
    _replyState = { messageId, authorName, preview };
    let bar = document.getElementById('reply-bar');
    if (!bar) {
        bar = document.createElement('div');
        bar.id = 'reply-bar';
        bar.className = 'reply-bar';
        const area = document.querySelector('.message-input-area');
        if (area) area.insertBefore(bar, area.firstChild);
    }
    bar.innerHTML = `<span class="reply-bar-text">Replying to <strong>${esc(authorName)}</strong>: ${esc(preview)}</span><button class="reply-bar-cancel" title="Cancel reply">&times;</button>`;
    bar.querySelector('.reply-bar-cancel').addEventListener('click', clearReplyState);
    bar.classList.remove('hidden');
}

function clearReplyState() {
    _replyState = null;
    const bar = document.getElementById('reply-bar');
    if (bar) bar.classList.add('hidden');
}

document.getElementById("message-form")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const input = document.getElementById("message-input");
    if (!input.value.trim()) return;
    let content = input.value;
    const replyId = _replyState?.messageId || null;
    // Prepend @mention for reply ping
    if (_replyState && _replyState.authorName) {
        const mention = '@' + _replyState.authorName;
        if (!content.startsWith(mention)) content = mention + ' ' + content;
    }
    try {
        await connection.invoke('SendChannelMessage', channelId, content, replyId, null);
        input.value = '';
        clearReplyState();
    } catch (err) {
        console.error('Failed to send message:', err);
    }
});

document.getElementById("dm-form")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const input = document.getElementById("dm-input");
    if (!input.value.trim()) return;
    await connection.invoke('SendDirectMessage', recipientId, input.value);
    input.value = '';
});

// Message deletion now handled via context menu (right-click / long-press)

// ── Burst / Rate Limit Warning ─────────────────────────────────
connection.on("BurstWarning", (message) => {
    // Show a transient toast in the message area
    const container = document.getElementById("messages");
    if (!container) return;
    const el = document.createElement('div');
    el.className = 'system-message burst-warning';
    el.style.cssText = 'color:#faa61a;font-size:13px;padding:4px 12px;font-style:italic;';
    el.textContent = '⚠ ' + message;
    container.appendChild(el);
    scrollToBottom();
    setTimeout(() => el.remove(), 5000);
});

// ── RPG Game Message Handler ────────────────────────────────────
connection.on("ReceiveGameMessage", (msg) => {
    const container = document.getElementById("messages");
    if (!container) return;
    const el = renderGameMessage(msg.type, msg.payload);
    if (el) {
        container.appendChild(el);
        scrollToBottom();
    }
});

function renderGameMessage(type, data) {
    const wrap = document.createElement('div');
    wrap.className = 'game-message' + (type === 'error' ? ' game-error' : '');

    switch (type) {
        case 'help':
            wrap.innerHTML = `<div class="game-title">${esc(data.title)}</div>
                <div class="help-list">${data.commands.map(c =>
                    `<span class="help-cmd">${esc(c.cmd)}</span><span class="help-desc">${esc(c.desc)}</span>`
                ).join('')}</div>`;
            break;

        case 'stats':
            const hpPct = Math.round(data.hp / data.maxHp * 100);
            const mpPct = Math.round(data.mp / data.maxMp * 100);
            const hpClass = hpPct < 25 ? 'low' : hpPct < 50 ? 'medium' : '';
            wrap.innerHTML = `
                <div class="game-title">${esc(data.name)} — Lv.${data.level} ${esc(data.className)}</div>
                <div class="game-section">
                    <span class="game-label">XP:</span> <span class="game-value">${data.xp} / ${data.xpToNext}</span>
                </div>
                <div class="hp-bar"><div class="hp-fill ${hpClass}" style="width:${hpPct}%"></div><span class="bar-text">${data.hp}/${data.maxHp} HP</span></div>
                <div class="mp-bar"><div class="mp-fill" style="width:${mpPct}%"></div><span class="bar-text">${data.mp}/${data.maxMp} MP</span></div>
                <div class="stat-grid">
                    ${statRow('STR', data.str, data.bonusStr)}${statRow('DEF', data.def, data.bonusDef)}
                    ${statRow('INT', data.int, data.bonusInt)}${statRow('DEX', data.dex, data.bonusDex)}
                    ${statRow('VIT', data.vit, data.bonusVit)}${statRow('LUK', data.luk, data.bonusLuk)}
                </div>
                <div class="game-section"><span class="game-label">Kills:</span> ${data.kills} | <span class="game-label">Deaths:</span> ${data.deaths}</div>`;
            break;

        case 'combat_start':
            wrap.innerHTML = `
                <div class="game-title">Combat Started!</div>
                <div class="combat-arena">
                    <div class="combatant"><span class="combatant-name">${esc(data.playerName)}</span>
                        <div class="hp-bar"><div class="hp-fill" style="width:${pct(data.playerHp, data.playerMaxHp)}%"></div><span class="bar-text">${data.playerHp}/${data.playerMaxHp}</span></div>
                    </div>
                    <span class="combat-vs">VS</span>
                    <div class="combatant"><span class="combatant-icon">${data.monsterIcon}</span>
                        <span class="combatant-name">${esc(data.monsterName)} Lv.${data.monsterLevel}</span>
                        <div class="hp-bar"><div class="hp-fill" style="width:100%"></div><span class="bar-text">${data.monsterHp}/${data.monsterMaxHp}</span></div>
                    </div>
                </div>
                <div class="game-section"><span class="game-label">Zone:</span> ${esc(data.monsterZone)} | <span class="game-label">Element:</span> ${esc(data.monsterElement)}</div>
                <div class="game-actions">
                    <button class="game-btn" onclick="gameCmd('/attack')">⚔️ Attack</button>
                    <button class="game-btn" onclick="gameCmd('/defend')">🛡️ Defend</button>
                    <button class="game-btn" onclick="gameCmd('/magic')">✨ Magic</button>
                    <button class="game-btn" onclick="gameCmd('/flee')">🏃 Flee</button>
                </div>`;
            break;

        case 'combat_turn':
            const pHpPct = pct(data.playerHp, data.playerMaxHp);
            const mHpPct = pct(data.monsterHp, data.monsterMaxHp);
            const pHpCls = pHpPct < 25 ? 'low' : pHpPct < 50 ? 'medium' : '';
            const mHpCls = mHpPct < 25 ? 'low' : mHpPct < 50 ? 'medium' : '';
            let html = `
                <div class="combat-arena">
                    <div class="combatant"><span class="combatant-name">${esc(data.playerName)}</span>
                        <div class="hp-bar"><div class="hp-fill ${pHpCls}" style="width:${pHpPct}%"></div><span class="bar-text">${data.playerHp}/${data.playerMaxHp}</span></div>
                        <div class="mp-bar"><div class="mp-fill" style="width:${pct(data.playerMp, data.playerMaxMp)}%"></div><span class="bar-text">${data.playerMp}/${data.playerMaxMp}</span></div>
                    </div>
                    <span class="combat-vs">⚔</span>
                    <div class="combatant"><span class="combatant-name">${esc(data.monsterName)}</span>
                        <div class="hp-bar"><div class="hp-fill ${mHpCls}" style="width:${mHpPct}%"></div><span class="bar-text">${data.monsterHp}/${data.monsterMaxHp}</span></div>
                    </div>
                </div>
                <ul class="combat-log">${data.log.map(l => `<li>${esc(l)}</li>`).join('')}</ul>`;

            if (data.state === 'AwaitingAction') {
                html += `<div class="game-actions">
                    <button class="game-btn" onclick="gameCmd('/attack')">⚔️ Attack</button>
                    <button class="game-btn" onclick="gameCmd('/defend')">🛡️ Defend</button>
                    <button class="game-btn" onclick="gameCmd('/magic')">✨ Magic</button>
                    <button class="game-btn" onclick="gameCmd('/flee')">🏃 Flee</button>
                </div>`;
            }

            if (data.combatResult) {
                const cr = data.combatResult;
                if (cr.result === 'victory') {
                    html += `<div class="level-up" style="border-color:#22c55e;">
                        <div style="font-size:1.1rem;font-weight:700;color:#22c55e;">Victory!</div>
                        <div>+${cr.xpGained} XP | +${cr.orbsGained} Orbs</div>`;
                    if (cr.loot && cr.loot.length > 0) {
                        html += `<div style="margin-top:4px;">Loot: ${cr.loot.map(l =>
                            `<span class="rarity-${l.rarity.toLowerCase()}">${l.quantity}x ${esc(l.name)}</span>`
                        ).join(', ')}</div>`;
                    }
                    if (cr.leveledUp) {
                        html += `<div class="level-up"><div class="level-up-text">LEVEL UP! → Lv.${cr.newLevel}</div></div>`;
                    }
                    html += `</div>`;
                } else if (cr.result === 'defeat') {
                    html += `<div class="level-up" style="border-color:#ef4444;background:linear-gradient(135deg,#ef444422,#dc262622);">
                        <div style="font-size:1.1rem;font-weight:700;color:#ef4444;">Defeat!</div>
                        <div>Lost ${cr.orbsLost} Orbs</div></div>`;
                }
            }

            wrap.innerHTML = html;
            break;

        case 'inventory':
            wrap.innerHTML = `<div class="game-title">Inventory</div>
                <ul class="inv-list">${data.items.length === 0 ? '<li>Empty</li>' : data.items.map(i =>
                    `<li class="inv-item"><span class="rarity-${i.rarity.toLowerCase()}">${esc(i.name)}${i.equipped ? '<span class="equipped-badge">' + esc(i.slot) + '</span>' : ''}</span><span>${i.quantity}x</span></li>`
                ).join('')}</ul>`;
            break;

        case 'equip':
            wrap.innerHTML = `<div class="game-title">Equipped</div>
                <div>${esc(data.item)} → ${esc(data.slot)}${data.unequipped ? ' (unequipped ' + esc(data.unequipped) + ')' : ''}</div>`;
            break;

        case 'unequip':
            wrap.innerHTML = `<div class="game-title">Unequipped</div><div>${esc(data.item)} from ${esc(data.slot)}</div>`;
            break;

        case 'gather':
            wrap.innerHTML = `<div class="game-title">${esc(data.action.charAt(0).toUpperCase() + data.action.slice(1))}!</div>
                <div class="gather-result"><span>Found ${data.quantity}x ${esc(data.item)} | +${data.xpGained} XP (Lv.${data.skillLevel})</span></div>`;
            break;

        case 'craft':
            wrap.innerHTML = `<div class="game-title">Crafting: ${esc(data.recipe)}</div>
                <div>${data.success
                    ? `Success! Got ${data.outputQty}x ${esc(data.outputItem)} | +${data.xpGained} XP`
                    : `Failed! Materials lost. +${data.xpGained} XP`}</div>`;
            break;

        case 'recipes':
            wrap.innerHTML = `<div class="game-title">Recipes</div>
                <ul class="inv-list">${data.recipes.map(r =>
                    `<li class="inv-item"><span>${esc(r.name)} → ${esc(r.output)}</span><span>${esc(r.skill)} Lv.${r.skillLevel}</span></li>`
                ).join('')}</ul>`;
            break;

        case 'leaderboard':
            wrap.innerHTML = `<div class="game-title">Leaderboard</div>
                <div class="game-section"><span class="game-label">By Level</span></div>
                <table class="lb-table"><tr><th>#</th><th>Name</th><th>Level</th></tr>
                ${data.byLevel.map(p => `<tr><td>${p.rank}</td><td>${esc(p.name)}</td><td>${p.level}</td></tr>`).join('')}</table>
                <div class="game-section" style="margin-top:8px"><span class="game-label">By Kills</span></div>
                <table class="lb-table"><tr><th>#</th><th>Name</th><th>Kills</th></tr>
                ${data.byKills.map(p => `<tr><td>${p.rank}</td><td>${esc(p.name)}</td><td>${p.kills}</td></tr>`).join('')}</table>`;
            break;

        case 'market_browse':
            wrap.innerHTML = `<div class="game-title">Market: ${esc(data.item)}</div>
                ${data.listings && data.listings.length > 0
                    ? data.listings.map(l => `<div class="market-listing"><span>${esc(l.seller)} — ${l.quantity}x</span><span class="market-price">${l.pricePerUnit} orbs/ea</span></div>`).join('')
                    : '<div>No listings found.</div>'}`;
            break;

        case 'market_listed':
            wrap.innerHTML = `<div class="game-title">Listed on Market</div><div>${data.quantity}x ${esc(data.item)} for ${data.price} orbs each</div>`;
            break;

        case 'market_bought':
            wrap.innerHTML = `<div class="game-title">Purchased!</div><div>${data.quantity}x ${esc(data.item)} for ${data.cost} orbs (${data.tax} tax)</div>`;
            break;

        case 'market_listings':
            wrap.innerHTML = `<div class="game-title">Your Listings</div>
                ${data.listings && data.listings.length > 0
                    ? data.listings.map(l => `<div class="market-listing"><span>${esc(l.item)} — ${l.quantity}x</span><span class="market-price">${l.pricePerUnit} orbs</span></div>`).join('')
                    : '<div>No active listings.</div>'}`;
            break;

        case 'market_cancelled':
            wrap.innerHTML = `<div class="game-title">Listing Cancelled</div><div>${esc(data.item)} returned to inventory.</div>`;
            break;

        case 'trade_offer':
            wrap.innerHTML = `<div class="game-title">Trade Offer</div><div>${esc(data.from)} → ${esc(data.to)}: ${data.quantity}x ${esc(data.item)}</div>
                <div class="game-actions">
                    <button class="game-btn" onclick="gameCmd('/trade accept')">Accept</button>
                    <button class="game-btn" onclick="gameCmd('/trade decline')">Decline</button>
                </div>`;
            break;

        case 'trade_accepted':
            wrap.innerHTML = `<div class="game-title">Trade Accepted!</div>`;
            break;

        case 'trade_declined':
            wrap.innerHTML = `<div class="game-title">Trade Declined</div>`;
            break;

        case 'game_config':
            wrap.innerHTML = `<div class="game-title">Game Config</div><div>${esc(data.message)}</div>`;
            break;

        case 'error':
            wrap.innerHTML = `<div>${esc(data.message)}</div>`;
            break;

        default:
            wrap.innerHTML = `<div>${JSON.stringify(data)}</div>`;
    }

    return wrap;
}

function gameCmd(cmd) {
    if (typeof connection !== 'undefined' && typeof channelId !== 'undefined') {
        connection.invoke('SendChannelMessage', channelId, cmd, null);
    }
}

function esc(str) {
    if (!str) return '';
    const d = document.createElement('div');
    d.textContent = str;
    return d.innerHTML;
}

function pct(val, max) { return max > 0 ? Math.round(val / max * 100) : 0; }

function statRow(name, base, bonus) {
    return `<div class="stat-item"><span class="stat-name">${name}</span><span class="stat-val">${base}${bonus > 0 ? ' <span class="stat-bonus">+' + bonus + '</span>' : ''}</span></div>`;
}

// ── Right-click / long-press context menu ─────────────────────────────
(function () {
    let _ctxMenu = null;
    let _longPressTimer = null;

    function dismissCtxMenu() {
        if (_ctxMenu) { _ctxMenu.remove(); _ctxMenu = null; }
    }

    document.addEventListener('click', dismissCtxMenu);
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape') dismissCtxMenu(); });

    const messagesContainer = document.getElementById('messages');
    if (!messagesContainer) return;

    function showContextMenu(messageEl, x, y) {
        const authorId = messageEl.dataset.authorId;
        const authorName = messageEl.dataset.authorName;
        const messageId = messageEl.dataset.id;
        if (!authorId || !authorName) return;

        const isSelf = typeof currentUserId !== 'undefined' && authorId === currentUserId;

        dismissCtxMenu();

        const role = typeof currentUserRole !== 'undefined' ? currentUserRole : 0;

        const menu = document.createElement('div');
        menu.className = 'context-menu';

        function addItem(label, icon, onClick, className) {
            const item = document.createElement('div');
            item.className = 'context-menu-item' + (className ? ' ' + className : '');
            item.innerHTML = `<span class="ctx-icon">${icon}</span><span>${label}</span>`;
            item.addEventListener('click', (ev) => {
                ev.stopPropagation();
                dismissCtxMenu();
                onClick();
            });
            menu.appendChild(item);
        }

        function addSeparator() {
            const sep = document.createElement('div');
            sep.className = 'context-menu-separator';
            menu.appendChild(sep);
        }

        // -- Reply action (everyone, only for channel messages with an id) --
        if (messageId) {
            addItem('Reply', '↩', () => {
                const preview = messageEl.querySelector('.message-content')?.textContent || '';
                const truncated = preview.length > 100 ? preview.substring(0, 100) + '...' : preview;
                setReplyState(messageId, authorName, truncated);
            });
            addSeparator();
        }

        // -- Social actions (everyone) --
        addItem('View Profile', '👤', () => {
            window.location.href = `/App/Profile?userId=${authorId}`;
        });

        if (!isSelf) {
            addItem('Send Message', '💬', () => {
                window.location.href = `/App/Index?friendId=${authorId}`;
            });

            addItem('Add Friend', '➕', async () => {
                try {
                    const resp = await fetch('/api/friends/request', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ username: authorName })
                    });
                    if (!resp.ok) {
                        const data = await resp.json().catch(() => ({}));
                        alert(data.error || 'Failed to send friend request.');
                    }
                } catch { alert('Failed to send friend request.'); }
            });

            addItem('Gift Orbs', '🎁', () => {
                const giftModal = document.getElementById('gift-modal');
                const recipient = document.getElementById('gift-recipient');
                if (giftModal && recipient) {
                    for (const opt of recipient.options) {
                        if (opt.value === authorName) { opt.selected = true; break; }
                    }
                    giftModal.classList.remove('hidden');
                }
            });

            addItem('Trade', '🔄', () => {
                if (typeof gameCmd === 'function') gameCmd(`/trade @${authorName}`);
            });

            addItem('Duel', '⚔️', () => {
                if (typeof gameCmd === 'function') gameCmd(`/duel @${authorName}`);
            });
        }

        // -- Report (non-self) --
        if (!isSelf && messageId) {
            addSeparator();
            addItem('Report Message', '🚩', () => {
                showReportModal(messageId);
            }, 'context-menu-danger');
        }

        // -- Moderation actions --
        if (role >= 1) {
            addSeparator();

            // Delete message (Moderator+)
            if (messageId && !messageEl.classList.contains('deleted')) {
                addItem('Delete Message', '🗑', async () => {
                    if (!confirm('Delete this message?')) return;
                    await connection.invoke('DeleteChannelMessage', serverId, channelId, messageId);
                }, 'context-menu-danger');
            }

            if (!isSelf) {
                addItem('Mute (10 min)', '🔇', async () => {
                    if (!confirm(`Mute ${authorName} for 10 minutes?`)) return;
                    try {
                        const resp = await fetch('/api/moderation/mute', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ serverId, targetUserId: authorId, durationMinutes: 10 })
                        });
                        if (!resp.ok) {
                            const data = await resp.json().catch(() => ({}));
                            alert(data.error || 'Failed to mute user.');
                        }
                    } catch { alert('Failed to mute user.'); }
                });
            }
        }

        if (!isSelf && role >= 2) {
            addItem('Kick', '👢', async () => {
                if (!confirm(`Kick ${authorName} from the server?`)) return;
                try {
                    const resp = await fetch('/api/moderation/kick', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ serverId, targetUserId: authorId })
                    });
                    if (!resp.ok) {
                        const data = await resp.json().catch(() => ({}));
                        alert(data.error || 'Failed to kick user.');
                    }
                } catch { alert('Failed to kick user.'); }
            }, 'context-menu-danger');

            addItem('Ban', '🚫', async () => {
                if (!confirm(`Ban ${authorName} from the server? This cannot be undone easily.`)) return;
                try {
                    const resp = await fetch('/api/moderation/ban', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ serverId, targetUserId: authorId })
                    });
                    if (!resp.ok) {
                        const data = await resp.json().catch(() => ({}));
                        alert(data.error || 'Failed to ban user.');
                    }
                } catch { alert('Failed to ban user.'); }
            }, 'context-menu-danger');
        }

        // Position menu with smart overflow handling
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';
        document.body.appendChild(menu);

        const rect = menu.getBoundingClientRect();
        const vw = window.innerWidth;
        const vh = window.innerHeight;

        // Horizontal: shift left if overflows right, clamp to 8px if overflows left
        if (rect.right > vw) menu.style.left = Math.max(8, vw - rect.width - 8) + 'px';
        if (rect.left < 0) menu.style.left = '8px';

        // Vertical: flip upward if overflows bottom
        if (rect.bottom > vh) {
            const flippedTop = y - rect.height;
            if (flippedTop >= 0) {
                menu.style.top = flippedTop + 'px';
            } else {
                // Clamp to top and make scrollable
                menu.style.top = '8px';
                menu.style.maxHeight = (vh - 16) + 'px';
                menu.style.overflowY = 'auto';
            }
        }

        _ctxMenu = menu;
    }

    // Desktop: right-click
    messagesContainer.addEventListener('contextmenu', (e) => {
        const messageEl = e.target.closest('.message');
        if (!messageEl) return;
        if (!messageEl.dataset.authorId) return;
        e.preventDefault();
        showContextMenu(messageEl, e.clientX, e.clientY);
    });

    // Mobile: long-press (500ms)
    messagesContainer.addEventListener('touchstart', (e) => {
        const messageEl = e.target.closest('.message');
        if (!messageEl || !messageEl.dataset.authorId) return;
        _longPressTimer = setTimeout(() => {
            _longPressTimer = null;
            const touch = e.touches[0];
            showContextMenu(messageEl, touch.clientX, touch.clientY);
        }, 500);
    }, { passive: true });

    messagesContainer.addEventListener('touchmove', () => {
        if (_longPressTimer) { clearTimeout(_longPressTimer); _longPressTimer = null; }
    }, { passive: true });

    messagesContainer.addEventListener('touchend', () => {
        if (_longPressTimer) { clearTimeout(_longPressTimer); _longPressTimer = null; }
    }, { passive: true });
})();

// ── Report Message Modal ──────────────────────────────────────────────────────
function showReportModal(messageId) {
    const existing = document.getElementById('report-modal-overlay');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.id = 'report-modal-overlay';
    overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.7);z-index:9999;display:flex;align-items:center;justify-content:center';

    overlay.innerHTML = `
        <div style="background:#313338;border-radius:8px;padding:24px;width:360px;max-width:90vw">
            <h3 style="margin:0 0 16px;color:#f2f3f5;font-size:16px">Report Message</h3>
            <label style="color:#b5bac1;font-size:13px;display:block;margin-bottom:6px">Reason</label>
            <select id="report-reason-select" style="width:100%;background:#1e1f22;color:#dcddde;border:1px solid #4e5058;border-radius:4px;padding:8px;margin-bottom:12px">
                <option value="Spam">Spam</option>
                <option value="Harassment">Harassment</option>
                <option value="Hate Speech">Hate Speech</option>
                <option value="Other">Other</option>
            </select>
            <label style="color:#b5bac1;font-size:13px;display:block;margin-bottom:6px">Additional note (optional)</label>
            <input id="report-note-input" type="text" maxlength="200" placeholder="Add context..."
                style="width:100%;box-sizing:border-box;background:#1e1f22;color:#dcddde;border:1px solid #4e5058;border-radius:4px;padding:8px;margin-bottom:16px" />
            <div style="display:flex;gap:8px;justify-content:flex-end">
                <button id="report-cancel-btn" class="btn btn-sm" style="background:#4e5058">Cancel</button>
                <button id="report-submit-btn" class="btn btn-sm btn-danger">Submit Report</button>
            </div>
            <p id="report-status" style="margin:8px 0 0;font-size:13px;color:#3ba55c;display:none"></p>
        </div>`;

    document.body.appendChild(overlay);

    document.getElementById('report-cancel-btn').addEventListener('click', () => overlay.remove());
    overlay.addEventListener('click', e => { if (e.target === overlay) overlay.remove(); });

    document.getElementById('report-submit-btn').addEventListener('click', async () => {
        const reason = document.getElementById('report-reason-select').value;
        const note = document.getElementById('report-note-input').value.trim() || null;
        const status = document.getElementById('report-status');

        try {
            const resp = await fetch('/api/report/message', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ messageId, reason, note })
            });
            if (resp.ok) {
                status.textContent = 'Report submitted. Thank you.';
                status.style.display = 'block';
                setTimeout(() => overlay.remove(), 1500);
            } else {
                status.textContent = 'Failed to submit report. Please try again.';
                status.style.color = '#ed4245';
                status.style.display = 'block';
            }
        } catch {
            status.textContent = 'Network error. Please try again.';
            status.style.color = '#ed4245';
            status.style.display = 'block';
        }
    });
}
