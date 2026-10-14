using System.Numerics;
using Core.Events;
using Core.Objects.Abstraction;
using Core.Systems.Abstraction;

namespace Core.Systems;


public sealed class CollisionSystem : ISimulationSystem
{
    private const float ContactTolerance = 1e-3f;
 
    private readonly List<ICollidable> _bodies = new();
 
    public void Update(SimulationContext ctx)
    {
        _bodies.Clear();
        _bodies.AddRange(ctx.Map.Query<ICollidable>());
 
        for (var i = 0; i < _bodies.Count; i++)
        {
            for (var j = i + 1; j < _bodies.Count; j++)
            {
                var a = _bodies[i];
                var b = _bodies[j];
 
                if (Overlap(a, b) && CanCollide(a, b))
                    ctx.Events.Publish(new TwoObjectsCollidedEvent(a, b));
            }
        }
    }
 
    private static bool Overlap(ICollidable a, ICollidable b)
    {
        var reach = a.Radius + b.Radius - ContactTolerance;
        return reach > 0 && Vector2.DistanceSquared(a.Position, b.Position) < reach * reach;
    }
 
    private static bool CanCollide(ICollidable a, ICollidable b) =>
        (a is IMovable || b is IMovable)             // два неподвижных друг на друга не реагируют: решать нечего
        && a.CollidesWith(b) && b.CollidesWith(a);   // вето любой из сторон отменяет столкновение
}