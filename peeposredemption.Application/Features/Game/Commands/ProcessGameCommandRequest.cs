using MediatR;

namespace peeposredemption.Application.Features.Game.Commands;

public class GameResponse
{
    public string Type { get; set; } = string.Empty;
    public object Payload { get; set; } = new { };
    public bool BroadcastToChannel { get; set; }
}

public class GameCommandResult
{
    public bool Handled { get; set; }
    public List<GameResponse> Responses { get; set; } = new();

    public static GameCommandResult NotHandled() => new() { Handled = false };

    public static GameCommandResult Single(string type, object payload, bool broadcast = false) =>
        new()
        {
            Handled = true,
            Responses = new List<GameResponse>
            {
                new() { Type = type, Payload = payload, BroadcastToChannel = broadcast }
            }
        };

    public static GameCommandResult Broadcast(string type, object payload) =>
        Single(type, payload, broadcast: true);
}

public record ProcessGameCommandRequest(
    Guid UserId,
    string Username,
    Guid ChannelId,
    string RawInput) : IRequest<GameCommandResult>;
