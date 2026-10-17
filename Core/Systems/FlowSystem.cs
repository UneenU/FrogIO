using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class FlowSystem : ISimulationSystem
{
    private const float alpha = 0.001f;

    private static Vector2 CircularForceField(Vector2 position, Vector2 center)
    {
        Vector2 radius = position - center;
        Vector2 tangent = new Vector2(-radius.Y, radius.X);

        return alpha * tangent;
    }

    public void Update(SimulationContext ctx)
    {
        var center = new Vector2(ctx.Map.Width / 2, ctx.Map.Height / 2);
        foreach (var movable in ctx.Map.Query<IMovable>())
        {
            if (movable is not WaterLily && movable is not Frog {IsInWater: true}) continue;
            
            var force = CircularForceField(movable.Position, center);
            movable.AddImpulse(force);
        }
    }
}