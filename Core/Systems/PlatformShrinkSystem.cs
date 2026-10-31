using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class PlatformShrinkSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        var platform = ctx.Map.Platform;
        platform.Shrink.Update(platform, ctx);
    }
}
