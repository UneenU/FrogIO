using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class FlowSystem
{
    public Vector2 GetForce(IMovable movable, SimulationContext ctx)
    {
        if (movable is not WaterLily && movable is not Frog { IsInWater: true }) return Vector2.Zero;

        var platform = ctx.Map.Platform;
        return platform.Flow.GetDisplacement(movable.Position, platform);
    }
}
