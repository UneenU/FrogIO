using System.Numerics;
using Core.Match;

namespace Core.Objects;


public sealed record FlySpawnRule(
    Type FlyType,
    Func<Vector2, Fly> Create,
    float ChancePerTick,
    int MaxAlive,
    TimeOfDay? OnlyAt = null,
    float DespawnChancePerTick = 0f)
{
    public static FlySpawnRule For<T>(
        Func<Vector2, T> create,
        float chancePerTick,
        int maxAlive,
        TimeOfDay? onlyAt = null,
        float despawnChancePerTick = 0f) where T : Fly =>
        new(typeof(T), create, chancePerTick, maxAlive, onlyAt, despawnChancePerTick);

    public bool IsInSeason(TimeOfDay time) => OnlyAt is null || OnlyAt == time;
}

public sealed record FlySpawnSettings
{
    public static FlySpawnSettings Default { get; } = new();

    public int MaxFliesTotal { get; init; } = 40;

    public float MinDistanceFromFrogs { get; init; } = 60f;

    public int MaxPositionAttempts { get; init; } = 8;

    public IReadOnlyList<FlySpawnRule> Rules { get; init; } =
    [
        FlySpawnRule.For(p => new PlainFly(p), chancePerTick: 0.004f, maxAlive: 15),
        FlySpawnRule.For(p => new Mosquito(p), chancePerTick: 0.002f, maxAlive: 10),
        FlySpawnRule.For(
            p => new Firefly(p),
            chancePerTick: 0.004f,
            maxAlive: 10,
            onlyAt: TimeOfDay.Night,
            despawnChancePerTick: 0.01f),
    ];
    
    public IReadOnlyList<FlyEffectSpawnRule> EffectRules { get; init; } =
    [
        new(() => new SpeedBoostEffect(duration: 600), 0.30f),
        new(() => new XpGainBoostEffect(duration: 600), 0.08f),
        new(() => new InstantXpEffect(amount: 500), 0.05f)
    ];
}


public sealed record FlyEffectSpawnRule(
    Func<Effect> Create,
    float Chance);