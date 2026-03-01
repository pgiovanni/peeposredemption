using MediatR;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Features.Messages.Queries
{

    public record GetConversationQuery(Guid UserId, Guid RecipientId, int Page, int PageSize) : IRequest<List<DirectMessageDto>>;
    public class GetConversationQueryHandler : IRequestHandler<GetConversationQuery, List<DirectMessageDto>>
    {
        private readonly IUnitOfWork _uow;

        public GetConversationQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<List<DirectMessageDto>> Handle(GetConversationQuery query, CancellationToken ct)
        {
            var messages = await _uow.DirectMessages.GetConversationAsync(
                query.UserId, query.RecipientId, query.Page, query.PageSize);

            return messages.Select(dm => new DirectMessageDto(
                dm.Id,
                dm.SenderId,
                dm.RecipientId,
                dm.Content,
                dm.SentAt
            )).ToList();
        }
    }
}
