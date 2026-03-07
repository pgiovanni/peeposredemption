using MediatR;
using peeposredemption.Application.DTOs.Channels;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Channels.Commands;

public record CreateChannelCommand(Guid ServerId, string Name) : IRequest<ChannelDto>;

public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, ChannelDto>
{
    private readonly IUnitOfWork _uow;
    public CreateChannelCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ChannelDto> Handle(CreateChannelCommand cmd, CancellationToken ct)
    {
        var channel = new Channel
        {
            ServerId = cmd.ServerId,
            Name = cmd.Name.ToLower().Replace(" ", "-")
        };

        await _uow.Channels.AddAsync(channel);
        await _uow.SaveChangesAsync();

        return new ChannelDto(channel.Id, channel.ServerId, channel.Name);
    }
}
