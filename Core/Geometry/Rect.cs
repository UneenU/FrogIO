using System.Numerics;

namespace Core.Geometry;

public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    public Rect Deflate(float distance) =>
        new(X + distance, Y + distance, Width - 2 * distance, Height - 2 * distance);
}
