namespace Core.Match;

/// <param name="TickRate">Тиков в секунду: по нему считаются Time и TimeLeft в MatchState.</param>
/// <param name="DurationSeconds">Длительность матча в секундах.</param>
public sealed record MatchSettings(int TickRate = 60, float DurationSeconds = 180f)
{
    public static MatchSettings Default { get; } = new();

    public float TickDuration => 1f / TickRate;
}
