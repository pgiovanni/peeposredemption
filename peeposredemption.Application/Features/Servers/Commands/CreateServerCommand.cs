using MediatR;
using peeposredemption.Application.DTOs.Servers;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Servers.Commands;

public record CreateServerCommand(Guid OwnerId, string Name, string? IconUrl)
    : IRequest<ServerDto>;

public class CreateServerCommandHandler : IRequestHandler<CreateServerCommand, ServerDto>
{
    private readonly IUnitOfWork _uow;

    public CreateServerCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ServerDto> Handle(CreateServerCommand cmd, CancellationToken ct)
    {
        var server = new Server
        {
            Name = cmd.Name,
            IconUrl = cmd.IconUrl,
            OwnerId = cmd.OwnerId
        };

        await _uow.Servers.AddAsync(server);

        // Every new server gets a default general channel
        var generalChannel = new Channel
        {
            ServerId = server.Id,
            Name = "general"
        };

        await _uow.Channels.AddAsync(generalChannel);

        // Owner is automatically a member of their own server
        var ownerMembership = new ServerMember
        {
            ServerId = server.Id,
            UserId = cmd.OwnerId
        };

        await _uow.Servers.AddMemberAsync(ownerMembership);
        await _uow.SaveChangesAsync();

        return new ServerDto(server.Id, server.Name, server.IconUrl);
    }
}
