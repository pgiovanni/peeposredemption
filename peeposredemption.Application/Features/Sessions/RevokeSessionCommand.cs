using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Sessions;

public record RevokeSessionCommand(Guid TokenId, Guid UserId) : IRequest<bool>;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RevokeSessionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(RevokeSessionCommand cmd, CancellationToken ct)
    {
        var token = await _uow.RefreshTokens.GetByIdAsync(cmd.TokenId);
        if (token == null || token.UserId != cmd.UserId || token.IsRevoked)
            return false;

        token.IsRevoked = true;
        await _uow.SaveChangesAsync();
        return true;
    }
}
