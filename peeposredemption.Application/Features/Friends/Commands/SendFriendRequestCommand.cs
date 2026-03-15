using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Friends.Commands;

public record SendFriendRequestCommand(Guid SenderId, string RecipientUsername) : IRequest<bool>;

public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public SendFriendRequestCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(SendFriendRequestCommand cmd, CancellationToken ct)
    {
        // Parental controls enforcement
        var parentalLink = await _uow.ParentalLinks.GetActiveByChildIdAsync(cmd.SenderId);
        if (parentalLink is { AccountFrozen: true })
            throw new InvalidOperationException("Your account is frozen by parental controls.");

        var recipient = await _uow.Users.GetByUsernameAsync(cmd.RecipientUsername);
        if (recipient == null || recipient.Id == cmd.SenderId) return false;

        if (await _uow.FriendRequests.ExistsAsync(cmd.SenderId, recipient.Id)) return false;

        await _uow.FriendRequests.AddAsync(new FriendRequest
        {
            SenderId = cmd.SenderId,
            ReceiverId = recipient.Id
        });

        await _uow.SaveChangesAsync();
        return true;
    }
}
