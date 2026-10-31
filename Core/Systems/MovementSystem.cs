using System.Numerics;
using Core.Events;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class MovementSystem(FlowSystem flowSystem) : ISimulationSystem
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

        // Фаза 1: все, кого никто не везёт, двигаются сами (в том числе носители: кувшинки).
        foreach (var obj in objects)
        {
            if (obj is IMovable movable && CarrierOf(obj) is null)
                _applied[obj] = Apply(obj, movable, movable.TotalVelocity, ctx);
        }

        // Фаза 2: пассажиры едут вместе с носителем. Собственная скорость тоже учитывается.
        foreach (var obj in objects)
        {
            if (obj is IMovable movable && CarrierOf(obj) is { } carrier)
                Apply(obj, movable, movable.TotalVelocity + _applied.GetValueOrDefault(carrier), ctx);
        }
    }

    /// <summary>Объект, который везёт <paramref name="obj"/> на себе (кувшинка под лягушкой), иначе <c>null</c>.</summary>
    private static GameObject? CarrierOf(GameObject obj) =>
        obj is Frog { Lily: GameObject lily } ? lily : null;

    private Vector2 Apply(GameObject obj, IMovable movable, Vector2 delta, SimulationContext ctx)
    {
        delta += flowSystem.GetForce(movable, ctx);
        if (delta == Vector2.Zero) return delta;

        movable.MoveBy(delta);

        var density = ctx.Map.GetDensityAtPosition(obj.Position);
        movable.DampDrift(density);

        return delta;
    }
}