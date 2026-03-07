using MediatR;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Features.Messages.Commands
{
    public record SendMessageCommand(Guid ChannelId, Guid AuthorId, string AuthorUsername, string Content)
    : IRequest<MessageDto>;

    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
    {
        private readonly IUnitOfWork _uow;
        public SendMessageCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
        {
            var message = new Message
            {
                ChannelId = cmd.ChannelId,
                AuthorId = cmd.AuthorId,
                Content = cmd.Content
            };
            await _uow.Messages.AddAsync(message);
            await _uow.SaveChangesAsync();
            return new MessageDto(message.Id, message.AuthorId, cmd.AuthorUsername, message.Content, message.SentAt);
        }
    }

}
