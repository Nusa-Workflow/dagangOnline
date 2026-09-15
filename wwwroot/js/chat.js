"use strict";

var chatConnection = new signalR.HubConnectionBuilder()
    .withUrl("/supportChatHub")
    .withAutomaticReconnect()
    .build();

var currentSessionId = null;

chatConnection.on("ReceiveMessage", function (sessionId, messageId, senderName, message, timestamp) {
    if (currentSessionId && currentSessionId !== sessionId) return;
    currentSessionId = sessionId;

    var isCurrentUser = senderName === "You" || senderName === "User";
    var msgClass = isCurrentUser ? "bg-primary text-white ms-auto" : (senderName === "System" ? "bg-warning text-dark mx-auto small text-center" : "bg-light border me-auto");
    var alignClass = isCurrentUser ? "justify-content-end" : (senderName === "System" ? "justify-content-center" : "justify-content-start");

    var date = new Date(timestamp);
    var timeString = date.getHours().toString().padStart(2, '0') + ':' + date.getMinutes().toString().padStart(2, '0');

    var html = `
        <div class="d-flex mb-3 ${alignClass}">
            <div class="p-2 rounded shadow-sm ${msgClass}" style="max-width: 75%;">
                ${senderName !== "User" && senderName !== "You" && senderName !== "System" ? `<strong class="d-block small mb-1">${senderName}</strong>` : ""}
                <span>${escapeHtml(message)}</span>
                <small class="d-block mt-1 ${isCurrentUser ? "text-white-50" : "text-muted"}" style="font-size: 0.7rem; text-align: right;">${timeString}</small>
            </div>
        </div>
    `;

    var chatBox = document.getElementById("chatMessages");
    if (chatBox) {
        chatBox.insertAdjacentHTML('beforeend', html);
        chatBox.scrollTop = chatBox.scrollHeight;
    }
});

chatConnection.on("AgentJoined", function (sessionId, agentId) {
    var escalateBtn = document.getElementById("btnEscalateChat");
    if (escalateBtn) {
        escalateBtn.style.display = "none";
    }
});

chatConnection.on("SessionEnded", function (sessionId) {
    if (currentSessionId === sessionId) {
        var chatInput = document.getElementById("chatMessageInput");
        var chatSendBtn = document.getElementById("btnSendChat");
        if (chatInput) chatInput.disabled = true;
        if (chatSendBtn) chatSendBtn.disabled = true;
    }
});

chatConnection.start().then(function () {
    console.log("Chat connected.");
}).catch(function (err) {
    return console.error(err.toString());
});

function escapeHtml(unsafe) {
    return (unsafe || "").toString()
         .replace(/&/g, "&amp;")
         .replace(/</g, "&lt;")
         .replace(/>/g, "&gt;")
         .replace(/"/g, "&quot;")
         .replace(/'/g, "&#039;");
}

function sendChatMessage() {
    var input = document.getElementById("chatMessageInput");
    var message = input.value;
    if (!message || message.trim() === "") return;

    if (!currentSessionId) {
        // Need a random Guid for the first message, backend will match or create
        currentSessionId = crypto.randomUUID();
    }

    // Checking if we are in agent view
    var isAgentView = document.getElementById("isAgentView");
    if (isAgentView && isAgentView.value === "true") {
        chatConnection.invoke("SendMessageToUser", currentSessionId, message).catch(function (err) {
            return console.error(err.toString());
        });
    } else {
        chatConnection.invoke("SendMessageToBot", currentSessionId, message).catch(function (err) {
            return console.error(err.toString());
        });
    }

    input.value = "";
    input.focus();
}

function requestEscalation() {
    if (currentSessionId) {
        chatConnection.invoke("RequestEscalation", currentSessionId).catch(function (err) {
            return console.error(err.toString());
        });
        
        var escalateBtn = document.getElementById("btnEscalateChat");
        if (escalateBtn) {
            escalateBtn.disabled = true;
            escalateBtn.innerText = "Eskalasi diminta...";
        }
    }
}

// Event listeners for UI
document.addEventListener("DOMContentLoaded", function () {
    var sendBtn = document.getElementById("btnSendChat");
    if (sendBtn) {
        sendBtn.addEventListener("click", function(e) {
            e.preventDefault();
            sendChatMessage();
        });
    }

    var chatInput = document.getElementById("chatMessageInput");
    if (chatInput) {
        chatInput.addEventListener("keypress", function(e) {
            if (e.key === "Enter") {
                e.preventDefault();
                sendChatMessage();
            }
        });
    }

    var escalateBtn = document.getElementById("btnEscalateChat");
    if (escalateBtn) {
        escalateBtn.addEventListener("click", function(e) {
            e.preventDefault();
            requestEscalation();
        });
    }
});
