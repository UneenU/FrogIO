namespace Core.Match;

public enum WeatherKind { Clear, Rain, Fog }

public sealed record WeatherState(WeatherKind Kind, int StartedAtTick, int EndsAtTick, float? VisibilityRadius)
{
    public static WeatherState Clear { get; } = new(WeatherKind.Clear, 0, 0, null);
}

public sealed record WeatherRule(float Frequency = 1f, float MeanDurationSeconds = 15f);

public sealed record WeatherSettings
{
    public static WeatherSettings Default { get; } = new();
    public WeatherRule Clear { get; init; } = new(3f, 30f);
    public WeatherRule Rain { get; init; } = new(1f, 15f);
    public WeatherRule Fog { get; init; } = new(1f, 15f);
    public float DurationVariation { get; init; } = 0.25f;
    public float FogVisibilityRadius { get; init; } = 100f;
}
