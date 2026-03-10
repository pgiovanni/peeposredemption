using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Services
{
    public static class StorageLimits
    {
        public const int FreeTierEmojiLimit = 50;
        public const int StandardTierEmojiLimit = 150;
        public const int BoostedTierEmojiLimit = 500;

        public static int GetLimit(StorageTier tier) => tier switch
        {
            StorageTier.Standard => StandardTierEmojiLimit,
            StorageTier.Boosted  => BoostedTierEmojiLimit,
            _                    => FreeTierEmojiLimit
        };

        public static string GetLabel(StorageTier tier) => tier switch
        {
            StorageTier.Standard => "Standard",
            StorageTier.Boosted  => "Boosted",
            _                    => "Free"
        };
    }
}
