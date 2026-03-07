using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Friends.Commands;

public record RespondFriendRequestCommand(Guid RequestId, Guid UserId, bool Accept) : IRequest<bool>;

public class RespondFriendRequestCommandHandler : IRequestHandler<RespondFriendRequestCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public RespondFriendRequestCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(RespondFriendRequestCommand cmd, CancellationToken ct)
    {
        var request = await _uow.FriendRequests.GetByIdAsync(cmd.RequestId);
        if (request == null || request.ReceiverId != cmd.UserId) return false;
        if (request.Status != FriendRequestStatus.Pending) return false;

        request.Status = cmd.Accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;
        await _uow.SaveChangesAsync();
        return true;
    }
}
