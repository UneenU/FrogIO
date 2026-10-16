using System.Numerics;

namespace Core.Geometry;

public static class Geometry
{
    public static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 p) =>
        Vector2.Distance(p, ClosestPointOnSegment(a, b, p));

    public static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();

        if (lengthSquared < Constants.Epsilon) return a;

        // Проекция p на прямую ab в долях длины отрезка; зажимаем в [0, 1], чтобы не выйти за концы.
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0f, 1f);
        return a + ab * t;
    }
}