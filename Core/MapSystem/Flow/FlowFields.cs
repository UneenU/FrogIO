using System.Numerics;

namespace Core.MapSystem;

public sealed class NoFlow : IFlowField
{
    public Vector2 GetDisplacement(Vector2 position, Platform platform) => Vector2.Zero;
}

public sealed class EdgeDriftFlow(float threshold = 50f, float speed = 0.15f) : IFlowField
{
    public Vector2 GetDisplacement(Vector2 position, Platform platform)
    {
        var b = platform.Bounds;
        return new Vector2(
            Axis(position.X, b.Left, b.Right),
            Axis(position.Y, b.Top, b.Bottom));
    }

    private float Axis(float value, float min, float max)
    {
        if (max - value < threshold) return speed;
        if (value - min < threshold) return -speed;
        return 0f;
    }
}

public sealed class VortexFlow(float alpha = 0.0004f, Vector2? center = null) : IFlowField
{
    public Vector2 GetDisplacement(Vector2 position, Platform platform)
    {
        var radius = position - (center ?? platform.Bounds.Center);
        var tangent = new Vector2(-radius.Y, radius.X);
        return alpha * tangent;
    }
}

public sealed class QuadrantFlow(float speed = 0.1f) : IFlowField
{
    public Vector2 GetDisplacement(Vector2 position, Platform platform)
    {
        var c = platform.Bounds.Center;
        return position switch
        {
            _ when position.X < c.X && position.Y < c.Y => new(speed, 0),
            _ when position.Y < c.Y => new(0, speed),
            _ when position.X > c.X => new(-speed, 0),
            _ => new(0, -speed)
        };
    }
}
