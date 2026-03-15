using MediatR;
using peeposredemption.Application.Features.Badges.Commands;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Servers.Commands;

public record JoinServerCommand(string Code, Guid UserId) : IRequest<Guid>;

public class JoinServerCommandHandler : IRequestHandler<JoinServerCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public JoinServerCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(JoinServerCommand cmd, CancellationToken ct)
    {
        // Parental controls enforcement
        var parentalLink = await _uow.ParentalLinks.GetActiveByChildIdAsync(cmd.UserId);
        if (parentalLink is { AccountFrozen: true })
            throw new InvalidOperationException("Your account is frozen by parental controls.");

        var invite = await _uow.ServerInvites.GetByCodeAsync(cmd.Code)
            ?? throw new Exception("Invite not found.");

        if (await _uow.BannedMembers.IsBannedAsync(invite.ServerId, cmd.UserId))
            throw new InvalidOperationException("You are banned from this server.");

        var alreadyMember = await _uow.Servers.IsMemberAsync(invite.ServerId, cmd.UserId);
        if (!alreadyMember)
        {
            await _uow.Servers.AddMemberAsync(new ServerMember
            {
                ServerId = invite.ServerId,
                UserId = cmd.UserId
            });
            await _uow.SaveChangesAsync();

            // Update server join stats + check badges
            var stats = await _mediator.Send(new UpdateActivityStatsCommand(cmd.UserId, IncrementServersJoined: 1), ct);
            await _mediator.Send(new CheckAndAwardBadgesCommand(cmd.UserId, "ServersJoined", stats.ServersJoined), ct);
        }

        return invite.ServerId;
    }
}
