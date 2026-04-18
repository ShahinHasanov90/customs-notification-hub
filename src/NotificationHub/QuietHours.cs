namespace NotificationHub;

public sealed record QuietHours(TimeOnly Start, TimeOnly End, string TimeZone)
{
    // Returns true when the current instant, projected into TimeZone, falls inside [Start, End].
    // Handles the wraparound case (e.g. 22:00 -> 07:00) by OR-ing the two disjoint sides.
    public bool IsWithin(DateTime utcNow)
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), tz);
        var now = TimeOnly.FromDateTime(local);

        if (Start == End)
        {
            return false;
        }

        if (Start < End)
        {
            return now >= Start && now < End;
        }

        return now >= Start || now < End;
    }
}
