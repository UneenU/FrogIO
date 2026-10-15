using Core.Events;
using Core.Objects;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class AttackSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        foreach (var frog in ctx.Map.Query<Frog>())
        {
            if (!frog.State.HasFlag(FrogState.Attacking)) continue;

            //TODO: сделать нормальную логику
            
            var flies = ctx.Map.ObjectsInCircle<Fly>(frog.TonguePosition, 4);
            foreach (var fly in flies)
            {
                fly.CaughtBy = frog;
            }
            
            if (!frog.AdvanceAttack(flies)) continue;
            
            foreach (var fly in frog.CaughtFlies)
            {
                ctx.Events.Publish(new FrogEatFlyEvent(frog, fly));
            }
            frog.EndAttack();
        }
    }
}