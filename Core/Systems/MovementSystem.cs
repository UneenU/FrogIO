using System.Numerics;
using Core.Events;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class MovementSystem() : ISimulationSystem
{
    private readonly Dictionary<GameObject, Vector2> _applied = new();

    public void Update(SimulationContext ctx)
    {
        _applied.Clear();
        var objects = ctx.Map.GameObjects;

        // Мухи двигаются за языком лягушки
        foreach (var fly in ctx.Map.Query<Fly>())
        {
            if (fly.CaughtBy is null) continue;
            Vector2 delta = fly.CaughtBy.TonguePosition - fly.Position;
            fly.MoveBy(delta);
        }

        // Фаза 1: все, кого никто не везёт, двигаются сами (в том числе платформы).
        foreach (var obj in objects)
        {
            if (obj is IMovable movable && PlatformOf(obj) is null)
                _applied[obj] = Apply(obj, movable, movable.TotalVelocity, ctx);
            
            // соударения с границами карты
            Vector2[] mapCorners =
            [
                new(0, 0),
                new(ctx.Map.Width, 0),
                new(ctx.Map.Width, ctx.Map.Height),
                new(0, ctx.Map.Height)
            ];
            var prev = mapCorners[3];
            for (int j = 0; j < 4; j++)
            {
                if (Geometry.Geometry.DistanceToSegment(prev, mapCorners[j], obj.Position) < obj.Radius)
                {
                    ctx.Events.Publish(new ObjectHitMapBorderEvent(obj, prev, mapCorners[j]));
                }
                prev = mapCorners[j];
            }
        }

        // Фаза 2: пассажиры едут вместе с платформой. Собственная скорость тоже учитывается.
        foreach (var obj in objects)
        {
            if (obj is IMovable movable && PlatformOf(obj) is { } platform)
                Apply(obj, movable, movable.TotalVelocity + _applied.GetValueOrDefault(platform), ctx);
        }
    }

    private static GameObject? PlatformOf(GameObject obj) =>
        obj is Frog { Lily: GameObject lily } ? lily : null;

    private Vector2 Apply(GameObject obj, IMovable movable, Vector2 delta, SimulationContext ctx)
    {
        if (delta == Vector2.Zero) return delta;

        movable.MoveBy(delta);

        var density = ctx.Map.GetDensityAtPosition(obj.Position);
        movable.DampDrift(density);

        return delta;
    }
}