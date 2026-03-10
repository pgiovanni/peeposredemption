const token = document.querySelector('meta[name="jwt"]')?.content;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();

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

connection.on("ReceiveChannelMessage", (msg) => {
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
