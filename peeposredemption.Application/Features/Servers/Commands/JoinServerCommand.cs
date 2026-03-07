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
        System.Diagnostics.Debugger.Break(); // BP3: JoinServerCommand received, looking up invite code

        var invite = await _uow.ServerInvites.GetByCodeAsync(cmd.Code)
            ?? throw new Exception("Invite not found.");

        System.Diagnostics.Debugger.Break(); // BP4: invite resolved to ServerId

        var alreadyMember = await _uow.Servers.IsMemberAsync(invite.ServerId, cmd.UserId);
        if (!alreadyMember)
        {
            await _uow.Servers.AddMemberAsync(new ServerMember
            {
                ServerId = invite.ServerId,
                UserId = cmd.UserId
            });
            await _uow.SaveChangesAsync();

            System.Diagnostics.Debugger.Break(); // BP5: ServerMember row inserted
        }

        return invite.ServerId;
    }
}
