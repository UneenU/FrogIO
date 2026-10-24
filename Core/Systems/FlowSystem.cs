using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class FlowSystem
{
    private const float alpha = 0.0004f;

    private static Vector2 CircularForceField(Vector2 position, Vector2 center)
    {
        Vector2 radius = position - center;
        Vector2 tangent = new Vector2(-radius.Y, radius.X);

        return alpha * tangent;
    }

    private static Vector2 CircularForceField2(Vector2 position, float width, float height)
    {
        return position switch
        {
            _ when position.X < width / 2 && position.Y < height / 2 => new(0.1f, 0),
            _ when position.Y < height / 2 => new(0, 0.1f),
            _ when position.X > width / 2 => new(-0.1f, 0),
            _ => new(0, -0.1f)
        };
    }

    private static Vector2 ForceNearEdges(Vector2 position, float width, float height, float threshold)
    {
        float xDir = 0;
        if (width - position.X < threshold) xDir = 0.15f;
        else if (position.X < threshold) xDir = -0.15f;
        float yDir = 0;
        if (height - position.Y < threshold) yDir = 0.15f;
        else if (position.Y < threshold) yDir = -0.15f;
        return new Vector2(xDir, yDir);
    }

    public Vector2 GetForce(IMovable movable, SimulationContext ctx)
    {
        if (movable is not WaterLily && movable is not Frog { IsInWater: true }) return Vector2.Zero;
        // var center = new Vector2(ctx.Map.Width / 2, ctx.Map.Height / 2);
        // var force = CircularForceField(movable.Position, center);
        // var force = CircularForceField2(movable.Position, ctx.Map.Width, ctx.Map.Height);
        var force = ForceNearEdges(movable.Position, ctx.Map.Width, ctx.Map.Height, 50);
        return force;
        // movable.AddImpulse(force);
    }
}