using System.Numerics;

namespace Core.Geometry;

public static class Geometry
{
    public static bool CirclesIntersect(Vector2 firstCenter, float firstRadius, Vector2 secondCenter, float secondRadius)
    {
        var reach = firstRadius + secondRadius;
        return Vector2.DistanceSquared(firstCenter, secondCenter) <= reach * reach;
    }

    public static bool CircleIntersectsCapsule(Vector2 center, float radius, Vector2 a, Vector2 b, float capsuleRadius) =>
        DistanceToSegment(a, b, center) <= radius + capsuleRadius;

    public static bool CircleIntersectsTriangle(Vector2 center, float radius, Vector2 a, Vector2 b, Vector2 c)
    {
        if (IsInsideTriangle(center, a, b, c))
            return true;

        var distance = MathF.Min(
            DistanceToSegment(a, b, center),
            MathF.Min(DistanceToSegment(b, c, center), DistanceToSegment(c, a, center)));
        return distance <= radius;
    }

    public static bool IsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        var ab = Cross(b - a, point - a);
        var bc = Cross(c - b, point - b);
        var ca = Cross(a - c, point - c);

        var hasNegative = ab < 0f || bc < 0f || ca < 0f;
        var hasPositive = ab > 0f || bc > 0f || ca > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    public static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 p) =>
        Vector2.Distance(p, ClosestPointOnSegment(a, b, p));

    public static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();

        if (lengthSquared < Constants.Epsilon) return a;

        var t = Math.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0f, 1f);
        return a + ab * t;
    }
}
