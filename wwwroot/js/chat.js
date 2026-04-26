"use strict";

document.addEventListener("DOMContentLoaded", function () {
    console.log("chat.js loaded");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .withAutomaticReconnect()
        .build();

    const messagesList = document.getElementById("messagesList");
    const sendBtn = document.getElementById("sendBtn");
    const messageInput = document.getElementById("messageInput");
    const userSelect = document.getElementById("userSelect");
    const fileInput = document.getElementById("fileInput");
    const sendFileBtn = document.getElementById("sendFileBtn");
    const usersListContainer = document.getElementById("usersListContainer");
    const chatHeader = document.getElementById("chatHeader");

    const renderedMessageIds = new Set();

    function appendMessageHtml(html) {
        const div = document.createElement("div");
        div.innerHTML = html;
        messagesList.appendChild(div);
        messagesList.scrollTop = messagesList.scrollHeight;
    }

    function appendMessageHtmlWithId(id, html) {
        if (id && renderedMessageIds.has(String(id))) return;
        if (id) renderedMessageIds.add(String(id));
        appendMessageHtml(html);
    }

    // Завантажити список користувачів і відобразити як опції та як список
    fetch("/Chat/UsersList")
        .then(r => r.json())
        .then(users => {
            users.forEach(u => {
                const opt = document.createElement("option");
                opt.value = u.id;
                opt.textContent = u.email + (u.isAdmin ? " (admin)" : "");
                userSelect.appendChild(opt);

                const a = document.createElement("a");
                a.href = "#";
                a.className = "list-group-item list-group-item-action";
                a.textContent = u.email + (u.isAdmin ? " (admin)" : "");
                a.dataset.userid = u.id;
                a.addEventListener("click", function (e) {
                    e.preventDefault();
                    userSelect.value = this.dataset.userid;
                    userSelect.dispatchEvent(new Event('change'));
                });
                usersListContainer.appendChild(a);
            });
        })
        .catch(err => console.error("UsersList fetch error:", err));

    function parseTimestamp(ts) {
        if (ts === null || ts === undefined) return "—";
        if (typeof ts === "number") {
            const d = new Date(ts);
            return isNaN(d.getTime()) ? "Invalid Date" : d.toLocaleString();
        }
        const num = Number(ts);
        if (!isNaN(num)) {
            const d = new Date(num);
            return isNaN(d.getTime()) ? "Invalid Date" : d.toLocaleString();
        }
        const dIso = new Date(ts);
        if (!isNaN(dIso.getTime())) {
            return dIso.toLocaleString();
        }
        const dm = String(ts).match(/^(\d{1,2})\.(\d{1,2})\.(\d{4})\s+(\d{1,2}):(\d{2}):(\d{2})$/);
        if (dm) {
            const day = parseInt(dm[1], 10);
            const month = parseInt(dm[2], 10) - 1;
            const year = parseInt(dm[3], 10);
            const hour = parseInt(dm[4], 10);
            const minute = parseInt(dm[5], 10);
            const second = parseInt(dm[6], 10);
            const d = new Date(year, month, day, hour, minute, second);
            return isNaN(d.getTime()) ? "Invalid Date" : d.toLocaleString();
        }
        const dFallback = new Date(ts);
        return isNaN(dFallback.getTime()) ? "Invalid Date" : dFallback.toLocaleString();
    }

    function getField(obj, ...names) {
        for (const n of names) {
            if (obj === null || obj === undefined) continue;
            if (Object.prototype.hasOwnProperty.call(obj, n)) return obj[n];
            const lower = n.charAt(0).toLowerCase() + n.slice(1);
            const upper = n.charAt(0).toUpperCase() + n.slice(1);
            if (Object.prototype.hasOwnProperty.call(obj, lower)) return obj[lower];
            if (Object.prototype.hasOwnProperty.call(obj, upper)) return obj[upper];
            if (Object.prototype.hasOwnProperty.call(obj, n.toLowerCase())) return obj[n.toLowerCase()];
        }
        return undefined;
    }

    connection.on("ReceiveMessage", (msg) => {
        console.log("ReceiveMessage raw:", msg);
        const id = getField(msg, "Id", "id");
        const senderEmail = getField(msg, "SenderEmail", "senderEmail");
        const messageText = getField(msg, "Message", "message");
        const timestampRaw = getField(msg, "Timestamp", "timestamp");
        const time = parseTimestamp(timestampRaw);
        const html = `<div class="mb-2"><strong>${escapeHtml(senderEmail)}</strong> <small class="text-muted">${time}</small><div>${escapeHtml(messageText)}</div></div>`;
        appendMessageHtmlWithId(id, html);
    });

    connection.on("ReceivePrivateMessage", (msg) => {
        console.log("ReceivePrivateMessage raw:", msg);
        const id = getField(msg, "Id", "id");
        const senderEmail = getField(msg, "SenderEmail", "senderEmail");
        const receiverEmail = getField(msg, "ReceiverEmail", "receiverEmail");
        const messageText = getField(msg, "Message", "message");
        const timestampRaw = getField(msg, "Timestamp", "timestamp");
        const time = parseTimestamp(timestampRaw);
        const targetLabel = receiverEmail ? ` → ${escapeHtml(receiverEmail)}` : " → (приватно)";
        const html = `<div class="mb-2"><strong>${escapeHtml(senderEmail)}${targetLabel}</strong> <small class="text-muted">${time}</small><div>${escapeHtml(messageText)}</div></div>`;
        appendMessageHtmlWithId(id, html);
    });

    connection.on("ReceiveFileMessage", (msg) => {
        console.log("ReceiveFileMessage raw:", msg);
        const id = getField(msg, "Id", "id");
        const senderEmail = getField(msg, "SenderEmail", "senderEmail");
        const fileName = getField(msg, "FileName", "fileName");
        const fileUrl = getField(msg, "FileUrl", "fileUrl");
        const receiverEmail = getField(msg, "ReceiverEmail", "receiverEmail");
        const timestampRaw = getField(msg, "Timestamp", "timestamp");
        const time = parseTimestamp(timestampRaw);
        const targetLabel = receiverEmail ? ` → ${escapeHtml(receiverEmail)}` : "";
        const fileLink = `<a href="${escapeAttr(fileUrl)}" target="_blank">${escapeHtml(fileName)}</a>`;
        const html = `<div class="mb-2"><strong>${escapeHtml(senderEmail)}${targetLabel}</strong> <small class="text-muted">${time}</small><div>${fileLink}</div></div>`;
        appendMessageHtmlWithId(id, html);
    });

    connection.on("LoadHistory", (messages) => {
        console.log("LoadHistory raw:", messages);
        messagesList.innerHTML = "";
        renderedMessageIds.clear();
        messages.forEach(msg => {
            console.log("History msg:", msg);
            const id = getField(msg, "Id", "id");
            const senderEmail = getField(msg, "SenderEmail", "senderEmail");
            const messageText = getField(msg, "Message", "message");
            const fileName = getField(msg, "FileName", "fileName");
            const fileUrl = getField(msg, "FileUrl", "fileUrl");
            const messageType = Number(getField(msg, "MessageType", "messageType") || 0);
            const receiverId = getField(msg, "ReceiverId", "receiverId");
            const receiverEmail = getField(msg, "ReceiverEmail", "receiverEmail");
            const timestampRaw = getField(msg, "Timestamp", "timestamp");
            const time = parseTimestamp(timestampRaw);

            let html;
            if (messageType === 0) {
                if (receiverId) {
                    const targetLabel = receiverEmail ? ` → ${escapeHtml(receiverEmail)}` : " → (приватно)";
                    html = `<div class="mb-2"><strong>${escapeHtml(senderEmail)}${targetLabel}</strong> <small class="text-muted">${time}</small><div>${escapeHtml(messageText)}</div></div>`;
                } else {
                    html = `<div class="mb-2"><strong>${escapeHtml(senderEmail)}</strong> <small class="text-muted">${time}</small><div>${escapeHtml(messageText)}</div></div>`;
                }
            } else {
                const fileLinkHtml = `<a href="${escapeAttr(fileUrl)}" target="_blank">${escapeHtml(fileName)}</a>`;
                html = `<div class="mb-2"><strong>${escapeHtml(senderEmail)}</strong> <small class="text-muted">${time}</small><div>${fileLinkHtml}</div></div>`;
            }
            appendMessageHtmlWithId(id, html);
        });
    });

    connection.start()
        .then(() => {
            console.log("SignalR connected");
            connection.invoke("GetRecentMessagesForConversation", null, 200).catch(err => console.error("GetRecentMessagesForConversation error:", err.toString()));
        })
        .catch(err => {
            console.error("SignalR connection error:", err.toString());
        });

    userSelect.addEventListener("change", function () {
        const otherUserId = userSelect.value || null;
        messagesList.innerHTML = "";
        renderedMessageIds.clear();
        const selectedText = userSelect.options[userSelect.selectedIndex].text;
        chatHeader.textContent = otherUserId ? ("Чат з: " + selectedText) : "Загальний чат";
        connection.invoke("GetRecentMessagesForConversation", otherUserId, 200).catch(err => console.error("GetRecentMessagesForConversation invoke error:", err.toString()));
    });

    sendBtn.addEventListener("click", function () {
        const text = messageInput.value.trim();
        if (!text) return;
        const receiverId = userSelect.value || null;
        if (receiverId) {
            connection.invoke("SendPrivateMessage", receiverId, text).catch(err => console.error("SendPrivateMessage error:", err.toString()));
        } else {
            connection.invoke("SendMessageToAll", text).catch(err => console.error("SendMessageToAll error:", err.toString()));
        }
        messageInput.value = "";
    });

    sendFileBtn.addEventListener("click", function () {
        const file = fileInput.files[0];
        if (!file) return alert("Оберіть файл");
        const form = new FormData();
        form.append("file", file);

        fetch("/Chat/UploadFile", {
            method: "POST",
            body: form
        })
            .then(r => {
                if (!r.ok) throw new Error("Upload failed");
                return r.json();
            })
            .then(data => {
                const receiverId = userSelect.value || null;
                connection.invoke("SendFileMessage", receiverId, data.fileName, data.fileUrl)
                    .catch(err => console.error("SendFileMessage error:", err.toString()));
                fileInput.value = "";
            })
            .catch(err => {
                console.error("UploadFile error:", err);
                alert("Помилка завантаження файлу");
            });
    });

    function escapeHtml(text) {
        if (!text) return "";
        return String(text).replace(/[&<>"'`=\/]/g, function (s) {
            return ({
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;',
                '/': '&#x2F;',
                '`': '&#x60;',
                '=': '&#x3D;'
            })[s];
        });
    }

    function escapeAttr(text) {
        if (!text) return "";
        return String(text).replace(/"/g, '&quot;');
    }
});
