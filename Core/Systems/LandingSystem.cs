using Core.Events;
using Core.Objects;
using Core.Systems.Interfaces;

namespace Core.Systems;

/// <summary>
/// Завершение прыжков. Идёт после MovementSystem, поэтому лягушка уже в конечной точке.
/// Читает карту и публикует события, поэтому живёт в системе, а не в Frog.
/// </summary>
public sealed class LandingSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        foreach (var frog in ctx.Map.Query<Frog>())
        {
            if (frog.State == FrogState.Jumping)
            {
                if (!frog.AdvanceJump()) continue;
                LandFrog(ctx, frog);
            }

            else if (frog.State.HasFlag(FrogState.SittingOnWaterLily) && frog.Lily is not null && !frog.Lily.Contains(frog.Position))
            {
                LandFrog(ctx, frog);
            }
        }
    }

    private void LandFrog(SimulationContext ctx, Frog frog)
    {
        var lily = ctx.Map.ObjectsAtPoint<WaterLily>(frog.Position).FirstOrDefault();
        frog.Land(lily);
        ctx.Events.Publish(lily is null
            ? new FrogFallInWaterEvent(frog)
            : new FrogLandOnWaterLilyEvent(frog, lily));
    }
}