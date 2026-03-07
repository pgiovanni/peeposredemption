const token = document.querySelector('meta[name="jwt"]')?.content;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveChannelMessage", (msg) => {
    const div = document.createElement("div");
    div.innerHTML = `<strong>${msg.authorUsername}</strong> ${msg.content}`;
    document.getElementById("messages")?.appendChild(div);
});

connection.on("ReceiveDirectMessage", (msg) => {
    const div = document.createElement("div");
    div.textContent = msg.content;
    document.getElementById("messages")?.appendChild(div);
});

connection.on("UserTyping", (userId) => {
    const el = document.getElementById("typing-indicator");
    if (el) el.textContent = userId + " is typing...";
    setTimeout(() => { if (el) el.textContent = ''; }, 2000);
});

(async () => {
    await connection.start();

    if (typeof channelId !== 'undefined')
        await connection.invoke('JoinChannel', serverId, channelId);
})();

document.getElementById("message-form")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const input = document.getElementById("message-input");
    await connection.invoke('SendChannelMessage', channelId, input.value);
    input.value = '';
});

document.getElementById("dm-form")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const input = document.getElementById("dm-input");
    await connection.invoke('SendDirectMessage', recipientId, input.value);
    input.value = '';
});
