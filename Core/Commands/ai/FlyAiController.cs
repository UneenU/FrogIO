using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Commands;

public sealed class FlyAiController(Fly controllable, Random? random = null) : IAiController<Fly>
{
    private const float FrogDetectionRadius = 40f;

    private readonly Random _random = random ?? new Random();
    private Vector2 _target;
    private int _nextDecisionTick;
    private int _fleeUntilTick;
    private bool _hasTarget;

    public Fly Controllable { get; } = controllable;

    public Command Think(SimulationContext ctx)
    {
        var fly = Controllable;
        var tick = ctx.Tick;

        if (fly.CaughtBy is not null)
            return MoveTo(fly.Position);

        var nearbyFrog = FlyAiHelpers.FindNearestFrog(fly, ctx.Map, FrogDetectionRadius);
        if (nearbyFrog is not null)
        {
            var distance = Vector2.Distance(fly.Position, nearbyFrog.Position);
            _fleeUntilTick = Math.Max(_fleeUntilTick, tick + _random.Next(12, 20));

            if (!_hasTarget || tick >= _nextDecisionTick || HasReachedTarget(fly))
            {
                _target = FlyAiHelpers.EscapeTarget(
                    fly, nearbyFrog, ctx.Map, _random, minDistance: 75f, maxDistance: 125f);
                _nextDecisionTick = tick + (distance < 55f ? _random.Next(3, 6) : _random.Next(7, 13));
                _hasTarget = true;
            }

            return MoveTo(_target);
        }
        
        if (tick < _fleeUntilTick && _hasTarget)
            return MoveTo(_target);

        if (!_hasTarget || tick >= _nextDecisionTick || HasReachedTarget(fly))
        {
            _target = ChooseWanderTarget(fly, ctx.Map);
            _nextDecisionTick = tick + _random.Next(65, 121);
            _hasTarget = true;
        }

        return MoveTo(_target);
    }

    private Vector2 ChooseWanderTarget(Fly fly, MapSystem.Map map)
    {
        if (_random.NextSingle() < 0.08f)
            return FlyAiHelpers.RandomMapPoint(fly, map, _random);

        var angle = _random.NextSingle() * MathF.Tau;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        direction = FlyAiHelpers.BiasAwayFromEdges(fly.Position, direction, map);

        var mapScale = MathF.Min(map.Width, map.Height);
        var maxDistance = Math.Clamp(mapScale * 0.16f, 55f, 120f);
        var distance = maxDistance * (0.45f + _random.NextSingle() * 0.55f);
        var target = fly.Position + direction * distance;

        return FlyAiHelpers.ClampInsideMap(target, fly, map);
    }

    private bool HasReachedTarget(Fly fly)
    {
        var arrivalDistance = MathF.Max(fly.Radius * 2f, 10f);
        return _hasTarget && Vector2.DistanceSquared(fly.Position, _target) <= arrivalDistance * arrivalDistance;
    }

    private Command MoveTo(Vector2 target) => new()
    {
        Controllable = Controllable,
        Target = target,
        Buttons = CommandButtons.Move
    };
}

internal static class FlyAiHelpers
{
    public static Frog? FindNearestFrog(Fly fly, MapSystem.Map map, float maxDistance)
    {
        var maxDistanceSquared = maxDistance * maxDistance;
        var nearestDistanceSquared = maxDistanceSquared;
        Frog? nearest = null;

        foreach (var frog in map.Query<Frog>())
        {
            if (!frog.IsAlive)
                continue;

            var distanceSquared = Vector2.DistanceSquared(fly.Position, frog.Position);
            if (distanceSquared >= nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            nearest = frog;
        }

        return nearest;
    }

    public static Vector2 EscapeTarget(
        Fly fly,
        Frog frog,
        MapSystem.Map map,
        Random random,
        float minDistance,
        float maxDistance)
    {
        var away = fly.Position - frog.Position;
        if (away.LengthSquared() < 1e-4f)
            away = -fly.Facing;
        else
            away = Vector2.Normalize(away);

        var side = new Vector2(-away.Y, away.X);
        var sideAmount = (random.NextSingle() * 2f - 1f) * 0.65f;
        var direction = Vector2.Normalize(away + side * sideAmount);
        direction = BiasAwayFromEdges(fly.Position, direction, map);

        var distance = minDistance + random.NextSingle() * (maxDistance - minDistance);
        return ClampInsideMap(fly.Position + direction * distance, fly, map);
    }

    public static Vector2 RandomMapPoint(Fly fly, MapSystem.Map map, Random random)
    {
        var margin = MathF.Max(fly.Radius + 2f, 8f);
        return new Vector2(
            RandomCoordinate(map.Width, margin, random),
            RandomCoordinate(map.Height, margin, random));
    }

    public static Vector2 ClampInsideMap(Vector2 point, Fly fly, MapSystem.Map map)
    {
        var margin = MathF.Max(fly.Radius + 2f, 8f);
        return new Vector2(
            ClampCoordinate(point.X, map.Width, margin),
            ClampCoordinate(point.Y, map.Height, margin));
    }

    public static Vector2 BiasAwayFromEdges(Vector2 position, Vector2 direction, MapSystem.Map map)
    {
        const float edgeBand = 55f;
        var inward = Vector2.Zero;

        if (position.X < edgeBand)
            inward.X += (edgeBand - position.X) / edgeBand;
        else if (position.X > map.Width - edgeBand)
            inward.X -= (position.X - (map.Width - edgeBand)) / edgeBand;

        if (position.Y < edgeBand)
            inward.Y += (edgeBand - position.Y) / edgeBand;
        else if (position.Y > map.Height - edgeBand)
            inward.Y -= (position.Y - (map.Height - edgeBand)) / edgeBand;

        var adjusted = direction + inward * 1.6f;
        return adjusted.LengthSquared() > 1e-4f ? Vector2.Normalize(adjusted) : direction;
    }

    private static float RandomCoordinate(float extent, float margin, Random random)
    {
        if (extent <= margin * 2f)
            return extent / 2f;

        return margin + random.NextSingle() * (extent - margin * 2f);
    }

    private static float ClampCoordinate(float value, float extent, float margin)
    {
        if (extent <= margin * 2f)
            return extent / 2f;

        return Math.Clamp(value, margin, extent - margin);
    }
}
