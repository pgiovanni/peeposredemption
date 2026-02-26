"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

window.onload = function() {
    if (localStorage.getItem('chatMessages' !== null)) {
        let messageArray = []
        localStorage.setItem('chatMessages', JSON.stringify(messageArray));
    } else {
        let onScreenMsgContainer = document.getElementById("messagesList");
        let messageArray = JSON.parse(localStorage.getItem("chatMessages"));
                
        messageArray.forEach(message => {
            var li = document.createElement("li");
            li.innerHTML = message;
            onScreenMsgContainer.appendChild(li)
        });
    }
}

connection.on("ReceiveMessage", function (user, message) {

    let existingMsgsJson = localStorage.getItem("chatMessages");
    let existingMsgsArray = JSON.parse(existingMsgsJson) || [];
    existingMsgsArray.push(user + ": " + message);
    let updatedMsgsJson = JSON.stringify(existingMsgsArray);
    localStorage.setItem('chatMessages', updatedMsgsJson);

    var li = document.createElement("li");        
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.innerHTML = `<b>${user}</b>: ${message}`;
    //document.getElementById("messages-user-submit").scrollTop = document.getElementById("messages-user-submit").scrollHeight;
    document.getElementById("messages-user-submit").scrollTo({
        left: 0,
        top: document.getElementById("messages-user-submit").scrollHeight,
        behavior: 'smooth'
    })
    const uhOh = new Audio("assets/ICQ Uh Oh!.mp3")
    uhOh.play(); 
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {

    if (document.getElementById("userInput").value == "") {
        alert("please enter a user name 😭")
        return;
    }
    else {
        document.getElementById("userInput").disabled = true
    }

    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", user, message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
    document.getElementById("messageInput").value = "";
    
    document.getElementById("messageInput").focus();
    
});