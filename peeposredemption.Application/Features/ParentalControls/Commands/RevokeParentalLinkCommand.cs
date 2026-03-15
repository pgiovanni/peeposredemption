using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.ParentalControls.Commands;

public record RevokeParentalLinkCommand(Guid UserId, Guid LinkId) : IRequest<Unit>;

public class RevokeParentalLinkCommandHandler : IRequestHandler<RevokeParentalLinkCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    public RevokeParentalLinkCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(RevokeParentalLinkCommand cmd, CancellationToken ct)
    {
        var link = await _uow.ParentalLinks.GetByIdAsync(cmd.LinkId)
            ?? throw new InvalidOperationException("Parental link not found.");

        if (link.ParentUserId != cmd.UserId && link.ChildUserId != cmd.UserId)
            throw new InvalidOperationException("You are not part of this parental link.");

        if (link.Status != ParentalLinkStatus.Active)
            throw new InvalidOperationException("This link is not active.");

        link.Status = ParentalLinkStatus.Revoked;

        await _uow.SaveChangesAsync();

        return Unit.Value;
    }
}
