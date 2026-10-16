namespace Core.Objects.Storks;

/// <summary>Параметры аиста и его спавна. Все расстояния в единицах карты, время в тиках.</summary>
public sealed record StorkSettings
{
    public static StorkSettings Default { get; } = new();

    public float Radius { get; } = 60;

    // --- Спавн ---
    public float SpawnChancePerTick { get; init; } = 0.002f;
    public int MinSpawnIntervalTicks { get; init; } = 100;

    // --- Полёт ---

    public float Speed { get; init; } = 1.2f;
    public float SpeedWileAttacking { get; init; } = 0.7f;
    public float CruiseHeight { get; init; } = 5 * 100;
    public int DescentTicks { get; init; } = 1 * Constants.TicksInSecond;
    public int AscentTicks { get; init; } = 3 * Constants.TicksInSecond;

    // --- Охота ---

    public float CaptureRadius { get; init; } = 18f;
    public float LookAheadDistance => SpeedWileAttacking * DescentTicks;
}