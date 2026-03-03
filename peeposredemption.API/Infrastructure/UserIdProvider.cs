using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace peeposredemption.API.Infrastructure
{
    public class UserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
            => connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
