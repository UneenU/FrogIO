using System.Numerics;
using Core.Events;
using Core.Objects.Abstraction;
using Core.Systems.Abstraction;

namespace Core.Systems;


public sealed class CollisionSystem : ISimulationSystem
{
    private const float ContactTolerance = 1e-3f;
 
 
    public void Update(SimulationContext ctx)
    {
        var bodies = ctx.Map.Query<ICollidable>();
 
        for (var i = 0; i < bodies.Count; i++)
        {
            for (var j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i];
                var b = bodies[j];
 
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