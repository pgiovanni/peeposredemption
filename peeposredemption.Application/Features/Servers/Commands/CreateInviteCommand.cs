using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Servers.Commands;

public record CreateInviteCommand(Guid ServerId, Guid CreatedByUserId) : IRequest<string>;

public class CreateInviteCommandHandler : IRequestHandler<CreateInviteCommand, string>
{
    private readonly IUnitOfWork _uow;
    public CreateInviteCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<string> Handle(CreateInviteCommand cmd, CancellationToken ct)
    {
        var invite = new ServerInvite
        {
            ServerId = cmd.ServerId,
            CreatedByUserId = cmd.CreatedByUserId
        };

        await _uow.ServerInvites.AddAsync(invite);
        await _uow.SaveChangesAsync();

        return invite.Code;
    }
}
