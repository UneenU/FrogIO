using System.Numerics;
using Core.Commands;
using Core.MapSystem;
using Core.Objects;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class FlySpawnSystem(
    AiInputSystem ai,
    AiControllerFactory aiFactory,
    FlySpawnSettings? settings = null,
    Random? random = null) : ISimulationSystem
{
    private const float EdgeMargin = 10f;

    private readonly FlySpawnSettings _settings = Validated(settings ?? FlySpawnSettings.Default);
    private readonly Random _random = random ?? new Random();

    public void Update(SimulationContext ctx)
    {
        DespawnOutOfSeason(ctx);
        Spawn(ctx);
    }

    private void Spawn(SimulationContext ctx)
    {
        var flies = ctx.Map.Query<Fly>();
        var total = flies.Count;

        foreach (var rule in _settings.Rules)
        {
            if (total >= _settings.MaxFliesTotal) return;
            if (!rule.IsInSeason(ctx.TimeOfDay)) continue;
            if (_random.NextSingle() >= rule.ChancePerTick) continue;
            if (CountOf(flies, rule) >= rule.MaxAlive) continue;
            if (!TryFindSpawnPoint(ctx.Map, out var point)) continue;

            var fly = rule.Create(point);
            fly.Effect = GetRandomEffect();
            
            ctx.Map.Spawn(fly);
            ai.Add(aiFactory.NewFlyAiController(fly, _random));
            total++;
        }
    }

    private Effect? GetRandomEffect()
    {
        var total = _random.NextSingle();
        foreach (var rule in _settings.EffectRules)
        {
            total -= rule.Chance;
            if (total <= 0)  return rule.Create();
        }

        return null;
    }

    private void DespawnOutOfSeason(SimulationContext ctx)
    {
        foreach (var rule in _settings.Rules)
        {
            if (rule.IsInSeason(ctx.TimeOfDay) || rule.DespawnChancePerTick <= 0f) continue;

            foreach (var fly in ctx.Map.Query<Fly>())
            {
                if (fly.GetType() != rule.FlyType || fly.CaughtBy is not null) continue;

                if (_random.NextSingle() < rule.DespawnChancePerTick)
                    ctx.Map.Despawn(fly);
            }
        }
    }

    private static int CountOf(IReadOnlyList<Fly> flies, FlySpawnRule rule)
    {
        var count = 0;
        foreach (var fly in flies)
            if (fly.GetType() == rule.FlyType)
                count++;
        return count;
    }

    private bool TryFindSpawnPoint(Map map, out Vector2 point)
    {
        for (var attempt = 0; attempt < _settings.MaxPositionAttempts; attempt++)
        {
            point = RandomEdgePoint(map);
            if (!IsNearAliveFrog(map, point))
                return true;
        }

        point = default;
        return false;
    }

    private Vector2 RandomEdgePoint(Map map)
    {
        var along = _random.NextSingle();
        var x = Math.Clamp(along * map.Width, EdgeMargin, Math.Max(EdgeMargin, map.Width - EdgeMargin));
        var y = Math.Clamp(along * map.Height, EdgeMargin, Math.Max(EdgeMargin, map.Height - EdgeMargin));

        return _random.Next(4) switch
        {
            0 => new Vector2(EdgeMargin, y),
            1 => new Vector2(Math.Max(EdgeMargin, map.Width - EdgeMargin), y),
            2 => new Vector2(x, EdgeMargin),
            _ => new Vector2(x, Math.Max(EdgeMargin, map.Height - EdgeMargin)),
        };
    }

    private bool IsNearAliveFrog(Map map, Vector2 point)
    {
        var min = _settings.MinDistanceFromFrogs;
        foreach (var frog in map.Query<Frog>())
            if (frog.IsAlive && Vector2.DistanceSquared(frog.Position, point) < min * min)
                return true;
        return false;
    }

    private static FlySpawnSettings Validated(FlySpawnSettings s)
    {
        if (s.MaxFliesTotal < 0 || s.MaxPositionAttempts <= 0 || s.MinDistanceFromFrogs < 0)
            throw new ArgumentException("Некорректные общие параметры спавна мух.", nameof(s));

        foreach (var rule in s.Rules)
        {
            if (rule.ChancePerTick is < 0f or > 1f || rule.DespawnChancePerTick is < 0f or > 1f || rule.MaxAlive < 0)
                throw new ArgumentException($"Некорректное правило спавна для {rule.FlyType.Name}.", nameof(s));
        }
        return s;
    }
}
