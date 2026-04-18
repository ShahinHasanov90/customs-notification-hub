namespace NotificationHub;

public sealed class RetryPolicy
{
    private readonly Random _rng;

    public RetryPolicy(
        int maxAttempts = 5,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        Random? rng = null)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        MaxAttempts = maxAttempts;
        BaseDelay = baseDelay ?? TimeSpan.FromSeconds(30);
        MaxDelay = maxDelay ?? TimeSpan.FromMinutes(15);
        if (BaseDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(baseDelay));
        if (MaxDelay < BaseDelay) throw new ArgumentOutOfRangeException(nameof(maxDelay));
        _rng = rng ?? Random.Shared;
    }

    public int MaxAttempts { get; }

    public TimeSpan BaseDelay { get; }

    public TimeSpan MaxDelay { get; }

    public bool ShouldRetry(int attemptCount) => attemptCount < MaxAttempts;

    // Full-jitter: pick a uniform value in [0, capped_delay].
    // Returns the ceiling (capped exponential delay) so callers can reason about the bound.
    public TimeSpan NextDelay(int attemptCount, out TimeSpan ceiling)
    {
        if (attemptCount < 1) throw new ArgumentOutOfRangeException(nameof(attemptCount));

        // 2^(attempt-1) saturates quickly; clamp the shift to avoid overflow.
        var shift = Math.Min(attemptCount - 1, 30);
        var expTicks = (long)(BaseDelay.Ticks * (1L << shift));
        if (expTicks < 0 || expTicks > MaxDelay.Ticks) expTicks = MaxDelay.Ticks;

        ceiling = TimeSpan.FromTicks(expTicks);
        var jitter = (long)(_rng.NextDouble() * expTicks);
        return TimeSpan.FromTicks(jitter);
    }

    public DateTime NextAttemptAt(int attemptCount, DateTime nowUtc)
    {
        var delay = NextDelay(attemptCount, out _);
        return nowUtc + delay;
    }
}
