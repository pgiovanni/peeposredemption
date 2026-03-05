using MediatR;
using peeposredemption.Application.DTOs.Channels;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Channels.Queries;

public record GetServerChannelsQuery(Guid ServerId) : IRequest<List<ChannelDto>>;

public class GetServerChannelsQueryHandler : IRequestHandler<GetServerChannelsQuery, List<ChannelDto>>
{
    private readonly IUnitOfWork _uow;
    public GetServerChannelsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ChannelDto>> Handle(GetServerChannelsQuery query, CancellationToken ct)
    {
        var channels = await _uow.Channels.GetServerChannelsAsync(query.ServerId);
        return channels.Select(c => new ChannelDto(c.Id, c.ServerId, c.Name)).ToList();
    }
}
