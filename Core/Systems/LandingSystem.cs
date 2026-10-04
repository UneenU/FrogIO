namespace Core;

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

                var lily = ctx.Map.ObjectsAtPoint<WaterLily>(frog.Position).FirstOrDefault();
                ctx.Events.Publish(frog.Land(lily));
            }

            else if (frog.State == FrogState.SittingOnWaterLily && !frog.Lily!.Contains(frog.Position))
            {
                var other = ctx.Map.ObjectsAtPoint<WaterLily>(frog.Position).FirstOrDefault();
                ctx.Events.Publish(frog.Land(other));
            }
        }
    }
}