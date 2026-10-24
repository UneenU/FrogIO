namespace Core.Match;

public sealed record MatchSettings(int TickRate = 60, float DurationSeconds = 180f, short WorldMinutesInOneSecond = 10)
{
    public static MatchSettings Default { get; } = new();

    public float TickDuration => 1f / TickRate;

    public Time DayStart = new Time(5, 0);
    public Time DayEnd = new Time(22, 0);
}
