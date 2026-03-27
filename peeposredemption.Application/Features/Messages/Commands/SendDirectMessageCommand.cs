using MediatR;
using Microsoft.Extensions.Caching.Memory;
using peeposredemption.Application.DTOs.Messages;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Messages.Commands
{
    public record SendDirectMessageCommand(Guid SenderId, Guid RecipientId, string Content)
    : IRequest<DirectMessageDto>;

    public class SendDirectMessageCommandHandler : IRequestHandler<SendDirectMessageCommand, DirectMessageDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILinkScannerService _linkScanner;
        private readonly IMemoryCache _cache;

        public SendDirectMessageCommandHandler(IUnitOfWork uow, ILinkScannerService linkScanner, IMemoryCache cache)
        {
            _uow = uow;
            _linkScanner = linkScanner;
            _cache = cache;
        }

        public async Task<DirectMessageDto> Handle(SendDirectMessageCommand cmd, CancellationToken ct)
        {
            // ── Parental controls ────────────────────────────────────────
            var parentalLink = await _uow.ParentalLinks.GetActiveByChildIdAsync(cmd.SenderId);
            if (parentalLink != null)
            {
                if (parentalLink.AccountFrozen)
                    throw new InvalidOperationException("Your account is frozen by parental controls.");

                if (parentalLink.DmFriendsOnly)
                {
                    var areFriends = await _uow.FriendRequests.ExistsAsync(cmd.SenderId, cmd.RecipientId);
                    if (!areFriends)
                        throw new InvalidOperationException("Parental controls restrict DMs to friends only.");
                }
            }

            var recipient = await _uow.Users.GetByIdAsync(cmd.RecipientId);
            if (recipient == null)
                throw new InvalidOperationException("Recipient not found.");

            var sender = await _uow.Users.GetByIdAsync(cmd.SenderId);
            if (sender == null)
                throw new InvalidOperationException("Sender not found.");

            // ── Scam detection ───────────────────────────────────────────

            // 1. Link scanning (reuse existing malicious link check)
            if (_linkScanner.ContainsMaliciousLink(cmd.Content))
                throw new InvalidOperationException("Your message contains a flagged link and cannot be sent.");

            // 2. New account DM blast: account < 7 days old AND > 8 unique recipients in last hour
            if ((DateTime.UtcNow - sender.CreatedAt).TotalDays < 7)
            {
                var since = DateTime.UtcNow.AddHours(-1);
                var recentCount = await _uow.DirectMessages.GetRecentRecipientCountAsync(cmd.SenderId, since);
                if (recentCount > 8)
                {
                    sender.IsSuspicious = true;
                    await _uow.SaveChangesAsync();
                    throw new InvalidOperationException("Your account has been flagged for sending too many DMs. Please contact support.");
                }
            }

            // 3. Tor + new account: account < 3 days old AND latest IP is Tor
            if ((DateTime.UtcNow - sender.CreatedAt).TotalDays < 3)
            {
                var ipLogs = await _uow.UserIpLogs.GetByUserIdAsync(cmd.SenderId);
                var latestLog = ipLogs.OrderByDescending(l => l.SeenAt).FirstOrDefault();
                if (latestLog?.IsTor == true)
                {
                    sender.IsSuspicious = true;
                    await _uow.SaveChangesAsync();
                    // Flag for review but don't block — fall through to send
                }
            }

            // 4. Repeated content: cache last 3 DM texts per user, flag on 3 identical matches
            var contentKey = $"dm_content:{cmd.SenderId}";
            var recentContents = _cache.GetOrCreate(contentKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return new List<string>();
            })!;

            int matchCount = recentContents.Count(c => c == cmd.Content);
            if (matchCount >= 3)
            {
                // Escalate: on 3rd+ repeat block the message
                sender.IsSuspicious = true;
                await _uow.SaveChangesAsync();
                throw new InvalidOperationException("Repeated message content detected. Please send a different message.");
            }

            // Keep only last 10 for rolling window
            recentContents.Add(cmd.Content);
            if (recentContents.Count > 10) recentContents.RemoveAt(0);

            // ── Persist the DM ───────────────────────────────────────────
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
}
