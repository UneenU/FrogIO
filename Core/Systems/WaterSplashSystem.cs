using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class WaterSplashSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        foreach (var splash in ctx.Map.Query<WaterSplash>())
        {
            if (splash.Advance())
                ctx.Map.Despawn(splash);
            foreach (var hittedObject in ctx.Map.ObjectsInCircle<IMovable>(splash.Position, splash.Radius))
            {
                var dir = (hittedObject as GameObject)!.Position - splash.Position;
                if (dir.LengthSquared() < 1e-6) continue;
                hittedObject.AddImpulse(splash.Strength / hittedObject.Mass * Vector2.Normalize(dir));
            }
        }
    }
}
