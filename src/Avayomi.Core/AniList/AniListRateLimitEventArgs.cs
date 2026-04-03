namespace Avayomi.Core.AniList;

public class AniListRateLimitEventArgs
{
    public int? RetryAfter { get; }
    public int RateLimit { get; }
    public int RateRemaining { get; }
    public DateTime? RateReset { get; }

    public AniListRateLimitEventArgs(
        int rateLimit,
        int rateRemaining,
        int? retryAfter = null,
        int? rateReset = null
    )
    {
        RateLimit = rateLimit;
        RateRemaining = rateRemaining;
        RetryAfter = retryAfter;
        RateReset =
            rateReset.HasValue ? DateTimeOffset.FromUnixTimeSeconds(rateReset.Value).DateTime
            : retryAfter.HasValue ? DateTimeOffset.UtcNow.AddSeconds(retryAfter.Value).DateTime
            : null;
    }
}
