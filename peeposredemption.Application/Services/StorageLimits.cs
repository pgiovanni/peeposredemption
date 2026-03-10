using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Services
{
    public static class StorageLimits
    {
        public const int FreeTierEmojiLimit = 10;
        public const int BoostedTierEmojiLimit = 100;

        public static int GetLimit(StorageTier tier) =>
            tier == StorageTier.Boosted ? BoostedTierEmojiLimit : FreeTierEmojiLimit;
    }
}
