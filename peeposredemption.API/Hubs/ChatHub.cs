using Microsoft.AspNetCore.SignalR;


namespace peeposredemption.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            var httpContext = Context.GetHttpContext();
            Console.WriteLine(httpContext.Connection.RemoteIpAddress.ToString());
            await Clients.All.SendAsync("ReceiveMessage", user, message);

        }
    }
}