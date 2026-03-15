using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.ParentalControls.Commands;

public record UpdateParentalControlsCommand(Guid ParentUserId, Guid LinkId, bool AccountFrozen, bool DmFriendsOnly) : IRequest<Unit>;

public class UpdateParentalControlsCommandHandler : IRequestHandler<UpdateParentalControlsCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    public UpdateParentalControlsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(UpdateParentalControlsCommand cmd, CancellationToken ct)
    {
        var link = await _uow.ParentalLinks.GetByIdAsync(cmd.LinkId)
            ?? throw new InvalidOperationException("Parental link not found.");

        if (link.ParentUserId != cmd.ParentUserId)
            throw new InvalidOperationException("You are not the parent on this link.");

        if (link.Status != ParentalLinkStatus.Active)
            throw new InvalidOperationException("This link is not active.");

        link.AccountFrozen = cmd.AccountFrozen;
        link.DmFriendsOnly = cmd.DmFriendsOnly;

        await _uow.SaveChangesAsync();

        return Unit.Value;
    }
}
