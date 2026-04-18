using NotificationHub;
using Xunit;

namespace NotificationHub.Tests;

public class RetryPolicyTests
{
    [Fact]
    public void Backoff_ceiling_grows_exponentially_until_capped()
    {
        var policy = new RetryPolicy(
            maxAttempts: 10,
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(64),
            rng: new Random(0));

        policy.NextDelay(1, out var c1);
        policy.NextDelay(2, out var c2);
        policy.NextDelay(3, out var c3);
        policy.NextDelay(4, out var c4);
        policy.NextDelay(7, out var cap);

        Assert.Equal(TimeSpan.FromSeconds(1), c1);
        Assert.Equal(TimeSpan.FromSeconds(2), c2);
        Assert.Equal(TimeSpan.FromSeconds(4), c3);
        Assert.Equal(TimeSpan.FromSeconds(8), c4);
        Assert.Equal(TimeSpan.FromSeconds(64), cap);
    }

    [Fact]
    public void ShouldRetry_honours_max_attempts()
    {
        var policy = new RetryPolicy(maxAttempts: 3);

        Assert.True(policy.ShouldRetry(1));
        Assert.True(policy.ShouldRetry(2));
        Assert.False(policy.ShouldRetry(3));
        Assert.False(policy.ShouldRetry(4));
    }

    [Fact]
    public void Jitter_stays_within_ceiling()
    {
        var policy = new RetryPolicy(
            maxAttempts: 10,
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(60),
            rng: new Random(1234));

        for (var attempt = 1; attempt <= 6; attempt++)
        {
            for (var i = 0; i < 50; i++)
            {
                var delay = policy.NextDelay(attempt, out var ceiling);
                Assert.True(delay >= TimeSpan.Zero);
                Assert.True(delay <= ceiling, $"delay {delay} exceeded ceiling {ceiling} at attempt {attempt}");
            }
        }
    }

    [Fact]
    public void NextAttemptAt_moves_forward()
    {
        var policy = new RetryPolicy(
            maxAttempts: 5,
            baseDelay: TimeSpan.FromSeconds(10),
            maxDelay: TimeSpan.FromMinutes(5),
            rng: new Random(42));
        var now = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var next = policy.NextAttemptAt(2, now);

        Assert.True(next >= now);
    }

    [Fact]
    public void Invalid_arguments_throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(maxAttempts: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RetryPolicy(baseDelay: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RetryPolicy(baseDelay: TimeSpan.FromMinutes(10), maxDelay: TimeSpan.FromMinutes(1)));
    }
}
