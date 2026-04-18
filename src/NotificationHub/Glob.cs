namespace NotificationHub;

// Glob matcher supporting '*' only. Intentionally does not use regex to keep
// behaviour trivially predictable for event-type routing.
public static class Glob
{
    public static bool IsMatch(string pattern, string value)
    {
        if (pattern is null) throw new ArgumentNullException(nameof(pattern));
        if (value is null) throw new ArgumentNullException(nameof(value));

        return MatchFrom(pattern, 0, value, 0);
    }

    private static bool MatchFrom(string pattern, int pi, string value, int vi)
    {
        while (pi < pattern.Length)
        {
            var pc = pattern[pi];
            if (pc == '*')
            {
                // Collapse runs of '*' - they're equivalent to a single one.
                while (pi < pattern.Length && pattern[pi] == '*') pi++;
                if (pi == pattern.Length) return true;

                // Try to match the remainder at every suffix of `value`.
                for (var k = vi; k <= value.Length; k++)
                {
                    if (MatchFrom(pattern, pi, value, k)) return true;
                }
                return false;
            }

            if (vi >= value.Length) return false;
            if (pc != value[vi]) return false;

            pi++;
            vi++;
        }

        return vi == value.Length;
    }
}
