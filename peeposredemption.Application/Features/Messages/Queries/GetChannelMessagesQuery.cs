using MediatR;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Features.Messages.Queries
{
    public record GetChannelMessagesQuery(Guid ChannelId, int Page = 1, int PageSize = 50)
     : IRequest<List<MessageDto>>;

    public class GetChannelMessagesQueryHandler
        : IRequestHandler<GetChannelMessagesQuery, List<MessageDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetChannelMessagesQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<List<MessageDto>> Handle(
            GetChannelMessagesQuery q, CancellationToken ct)
        {
            var messages = await _uow.Messages
                .GetChannelMessagesAsync(q.ChannelId, q.Page, q.PageSize);
            return messages
                .Select(m => new MessageDto(m.Id, m.AuthorId, m.Author.Username, m.Content, m.SentAt))
                .ToList();
        }
    }
}
