using MediatR;
using peeposredemption.Application.DTOs.Emoji;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Emoji.Queries
{
    public record GetServerEmojisQuery(Guid ServerId) : IRequest<List<ServerEmojiDto>>;

    public class GetServerEmojisQueryHandler : IRequestHandler<GetServerEmojisQuery, List<ServerEmojiDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetServerEmojisQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<List<ServerEmojiDto>> Handle(GetServerEmojisQuery query, CancellationToken ct)
        {
            var emojis = await _uow.ServerEmojis.GetByServerIdAsync(query.ServerId);
            return emojis.Select(e => new ServerEmojiDto(e.Id, e.Name, e.ImageUrl, e.ServerId, "")).ToList();
        }
    }
}
