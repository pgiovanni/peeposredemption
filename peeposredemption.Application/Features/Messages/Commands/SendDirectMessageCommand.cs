using MediatR;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Features.Messages.Commands
{
    public record SendDirectMessageCommand(Guid SenderId, Guid RecipientId, string Content)
    : IRequest<DirectMessageDto>;

    public class SendDirectMessageCommandHandler : IRequestHandler<SendDirectMessageCommand, DirectMessageDto>
    {
        private readonly IUnitOfWork _uow;
        public SendDirectMessageCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<DirectMessageDto> Handle(SendDirectMessageCommand cmd, CancellationToken ct)
        {
            var recipientExists = await _uow.Users.GetByIdAsync(cmd.RecipientId);
            if (recipientExists == null)
                throw new InvalidOperationException("Recipient not found.");

            var dm = new DirectMessage
            {
                SenderId = cmd.SenderId,
                RecipientId = cmd.RecipientId,
                Content = cmd.Content
            };

            await _uow.DirectMessages.AddAsync(dm);
            await _uow.SaveChangesAsync();

            return new DirectMessageDto(dm.Id, dm.SenderId, dm.RecipientId, dm.Content, dm.SentAt);
        }
    }

