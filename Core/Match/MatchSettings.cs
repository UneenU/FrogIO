namespace Core.Match;

public sealed record MatchSettings(int TickRate = 60, float DurationSeconds = 180f)
{
    public static MatchSettings Default { get; } = new();

    public float TickDuration => 1f / TickRate;
}
