using System.Numerics;
using Core.Events;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class AttackSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        var frogs = ctx.Map.Query<Frog>().ToList();
        foreach (var frog in frogs)
        {
            if (!frog.State.HasFlag(FrogState.Attacking)) continue;

            //TODO: сделать нормальную логику
            
            List<GameObject> objects = [];
            
            // мухи
            var flies = ctx.Map.ObjectsInCircle<Fly>(frog.TonguePosition, 3);
            foreach (var fly in flies)
            {
                fly.CaughtBy = frog;
            }
            objects.AddRange(flies);

            foreach (var other in frogs)
            {
                if (ReferenceEquals(other, frog)) continue;
                if (Vector2.Distance(frog.TonguePosition, other.Position) < other.Radius)
                {
                    var impulse = 2;
                    var dir = Vector2.Normalize(frog.Position - other.Position);
                    other.AddImpulse(impulse * dir);
                    frog.AddImpulse(-impulse * dir);
                    
                    objects.Add(other);
                }
            }

            if (!frog.AdvanceAttack(objects)) continue;

            foreach (var fly in frog.CaughtFlies)
            {
                ctx.Events.Publish(new FrogEatFlyEvent(frog, fly));
            }

            frog.EndAttack();
        }
    }
}