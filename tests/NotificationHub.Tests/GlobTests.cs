using NotificationHub;
using Xunit;

namespace NotificationHub.Tests;

public class GlobTests
{
    [Theory]
    [InlineData("customs.*", "customs.declaration.accepted", true)]
    [InlineData("customs.*", "sanctions.hit", false)]
    [InlineData("*", "anything.goes", true)]
    [InlineData("customs.declaration.accepted", "customs.declaration.accepted", true)]
    [InlineData("customs.declaration.accepted", "customs.declaration.rejected", false)]
    public void Matches_simple_cases(string pattern, string value, bool expected)
    {
        Assert.Equal(expected, Glob.IsMatch(pattern, value));
    }

    [Fact]
    public void Star_at_both_ends_matches_substring()
    {
        Assert.True(Glob.IsMatch("*.duty.*", "customs.duty.assessed"));
    }

    [Fact]
    public void Multiple_stars_collapse()
    {
        Assert.True(Glob.IsMatch("**customs**", "customs.something"));
        Assert.True(Glob.IsMatch("**customs**", "pre.customs.post"));
    }

    [Fact]
    public void Empty_pattern_matches_empty_only()
    {
        Assert.True(Glob.IsMatch(string.Empty, string.Empty));
        Assert.False(Glob.IsMatch(string.Empty, "x"));
    }

    [Fact]
    public void Star_only_matches_empty_and_anything()
    {
        Assert.True(Glob.IsMatch("*", string.Empty));
        Assert.True(Glob.IsMatch("*", "customs.anything.at.all"));
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => Glob.IsMatch(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => Glob.IsMatch("x", null!));
    }
}
