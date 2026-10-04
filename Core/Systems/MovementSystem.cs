using System.Numerics;

namespace Core;

/// <summary>
/// Кинематика: смещение = Velocity (единицы за тик), урезанное ограничениями.
/// Объекты сами позицию не меняют: система считает смещение и просит применить его через MoveBy.
/// </summary>
public sealed class MovementSystem() : ISimulationSystem
{
    // Смещения, реально применённые в этом тике (по Id объекта). Нужны, чтобы перевозить пассажиров.
    private readonly Dictionary<int, Vector2> _applied = new();
 
    public void Update(SimulationContext ctx)
    {
        _applied.Clear();
        var objects = ctx.Map.GameObjects;
 
        // Фаза 1: все, кого никто не везёт, двигаются сами (в том числе платформы).
        foreach (var obj in objects)
        {
            if (obj is IMovable movable && PlatformOf(obj) is null)
                _applied[obj.Id] = Apply(obj, movable, movable.TotalVelocity, ctx);
        }
 
        // Фаза 2: пассажиры едут вместе с платформой. Собственная скорость тоже учитывается.
        foreach (var obj in objects)
        {
            if (obj is IMovable movable && PlatformOf(obj) is { } platform)
                Apply(obj, movable, movable.TotalVelocity + _applied.GetValueOrDefault(platform.Id), ctx);
        }
    }
 
    /// <summary>Объект, который везёт obj в этом тике (сейчас: кувшинка под сидящей лягушкой).</summary>
    private static GameObject? PlatformOf(GameObject obj) =>
        obj is Frog { Lily: GameObject lily } ? lily : null;
 
    private Vector2 Apply(GameObject obj, IMovable movable, Vector2 delta, SimulationContext ctx)
    {
        if (delta != Vector2.Zero)
            movable.MoveBy(delta);
        
        var density = ctx.Map.GetDensityAtPosition(obj.Position);
        movable.DampDrift(density);
 
        return delta;
    }
}
