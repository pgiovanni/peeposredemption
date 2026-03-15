using MediatR;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Services;
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
        private readonly ILinkScannerService _scanner;
        private readonly IEmailService _email;
        public SendMessageCommandHandler(IUnitOfWork uow, ILinkScannerService scanner, IEmailService email)
        {
            _uow = uow;
            _scanner = scanner;
            _email = email;
        }

        public async Task<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
        {
            // Parental controls enforcement
            var parentalLink = await _uow.ParentalLinks.GetActiveByChildIdAsync(cmd.AuthorId);
            if (parentalLink is { AccountFrozen: true })
                throw new InvalidOperationException("Your account is frozen by parental controls.");

            if (_scanner.ContainsMaliciousLink(cmd.Content))
            {
                _ = _email.SendMaliciousLinkAlertAsync(cmd.AuthorUsername, cmd.ChannelId, cmd.Content);
                throw new InvalidOperationException("Message contains a blocked link.");
            }

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
