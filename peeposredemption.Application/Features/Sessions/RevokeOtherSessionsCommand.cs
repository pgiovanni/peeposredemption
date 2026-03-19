using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Sessions;

public record RevokeOtherSessionsCommand(Guid UserId, string CurrentTokenHash) : IRequest<int>;

public class RevokeOtherSessionsCommandHandler : IRequestHandler<RevokeOtherSessionsCommand, int>
{
    private readonly IUnitOfWork _uow;

    public RevokeOtherSessionsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(RevokeOtherSessionsCommand cmd, CancellationToken ct)
    {
        var sessions = await _uow.RefreshTokens.GetActiveSessionsAsync(cmd.UserId);
        var count = 0;

        foreach (var session in sessions)
        {
            if (session.Token != cmd.CurrentTokenHash)
            {
                session.IsRevoked = true;
                count++;
            }
        }

        if (count > 0)
            await _uow.SaveChangesAsync();

        return count;
    }
}
