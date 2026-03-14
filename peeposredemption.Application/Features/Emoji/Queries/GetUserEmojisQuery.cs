using MediatR;
using peeposredemption.Application.DTOs.Emoji;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Emoji.Queries
{
    public record GetUserEmojisQuery(Guid UserId) : IRequest<List<ServerEmojiDto>>;

    public class GetUserEmojisQueryHandler : IRequestHandler<GetUserEmojisQuery, List<ServerEmojiDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetUserEmojisQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<List<ServerEmojiDto>> Handle(GetUserEmojisQuery query, CancellationToken ct)
        {
            var emojis = await _uow.ServerEmojis.GetByUserServersAsync(query.UserId);
            return emojis.Select(e => new ServerEmojiDto(e.Id, e.Name, e.ImageUrl, e.ServerId, e.Server.Name)).ToList();
        }
    }
}
