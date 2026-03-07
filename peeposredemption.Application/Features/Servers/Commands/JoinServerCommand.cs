using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Servers.Commands;

public record JoinServerCommand(string Code, Guid UserId) : IRequest<Guid>;

public class JoinServerCommandHandler : IRequestHandler<JoinServerCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public JoinServerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(JoinServerCommand cmd, CancellationToken ct)
    {
        var invite = await _uow.ServerInvites.GetByCodeAsync(cmd.Code)
            ?? throw new Exception("Invite not found.");

        var alreadyMember = await _uow.Servers.IsMemberAsync(invite.ServerId, cmd.UserId);
        if (!alreadyMember)
        {
            await _uow.Servers.AddMemberAsync(new ServerMember
            {
                ServerId = invite.ServerId,
                UserId = cmd.UserId
            });
            await _uow.SaveChangesAsync();

        }

        return invite.ServerId;
    }
}
