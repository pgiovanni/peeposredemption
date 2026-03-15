using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Badges.Commands;

public record SeedBadgeDefinitionsCommand : IRequest;

public class SeedBadgeDefinitionsCommandHandler : IRequestHandler<SeedBadgeDefinitionsCommand>
{
    private readonly IUnitOfWork _uow;
    public SeedBadgeDefinitionsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(SeedBadgeDefinitionsCommand cmd, CancellationToken ct)
    {
        if (await _uow.BadgeDefinitions.CountAsync() > 0) return;

        var badges = new List<BadgeDefinition>
        {
            // Activity — Messages
            new() { Name = "First Steps", Description = "Send your first message", Icon = "\ud83d\udc63", Category = BadgeCategory.Activity, StatKey = "TotalMessages", Threshold = 1, OrbReward = 5, SortOrder = 1 },
            new() { Name = "Chatterbox", Description = "Send 1,000 messages", Icon = "\ud83d\udcac", Category = BadgeCategory.Activity, StatKey = "TotalMessages", Threshold = 1000, OrbReward = 50, SortOrder = 2 },
            new() { Name = "Wordsmith", Description = "Send 10,000 messages", Icon = "\u270d\ufe0f", Category = BadgeCategory.Activity, StatKey = "TotalMessages", Threshold = 10000, OrbReward = 250, SortOrder = 3 },
            new() { Name = "Legend", Description = "Send 100,000 messages", Icon = "\ud83d\udc51", Category = BadgeCategory.Activity, StatKey = "TotalMessages", Threshold = 100000, OrbReward = 1000, SortOrder = 4 },

            // Activity — Streaks
            new() { Name = "Dedicated", Description = "Reach a 7-day login streak", Icon = "\ud83d\udd25", Category = BadgeCategory.Activity, StatKey = "LongestStreak", Threshold = 7, OrbReward = 25, SortOrder = 5 },
            new() { Name = "Committed", Description = "Reach a 30-day login streak", Icon = "\u26a1", Category = BadgeCategory.Activity, StatKey = "LongestStreak", Threshold = 30, OrbReward = 150, SortOrder = 6 },
            new() { Name = "Devoted", Description = "Reach a 100-day login streak", Icon = "\ud83c\udf1f", Category = BadgeCategory.Activity, StatKey = "LongestStreak", Threshold = 100, OrbReward = 500, SortOrder = 7 },

            // Social — Gifting
            new() { Name = "Generous", Description = "Gift a total of 100 orbs", Icon = "\ud83c\udf81", Category = BadgeCategory.Social, StatKey = "TotalOrbsGifted", Threshold = 100, OrbReward = 25, SortOrder = 8 },
            new() { Name = "Philanthropist", Description = "Gift a total of 10,000 orbs", Icon = "\ud83d\udc8e", Category = BadgeCategory.Social, StatKey = "TotalOrbsGifted", Threshold = 10000, OrbReward = 500, SortOrder = 9 },

            // Social — Servers
            new() { Name = "Social Butterfly", Description = "Join 5 servers", Icon = "\ud83e\udd8b", Category = BadgeCategory.Social, StatKey = "ServersJoined", Threshold = 5, OrbReward = 25, SortOrder = 10 },
            new() { Name = "Networker", Description = "Join 15 servers", Icon = "\ud83c\udf10", Category = BadgeCategory.Social, StatKey = "ServersJoined", Threshold = 15, OrbReward = 100, SortOrder = 11 },

            // Economy
            new() { Name = "First Orb", Description = "Earn your first orb", Icon = "\u2728", Category = BadgeCategory.Economy, StatKey = "PeakOrbBalance", Threshold = 1, OrbReward = 10, SortOrder = 12 },
            new() { Name = "Hoarder", Description = "Hold 10,000 orbs at once", Icon = "\ud83d\udcb0", Category = BadgeCategory.Economy, StatKey = "PeakOrbBalance", Threshold = 10000, OrbReward = 500, SortOrder = 13 },
        };

        foreach (var badge in badges)
            await _uow.BadgeDefinitions.AddAsync(badge);

        await _uow.SaveChangesAsync();
    }
}
