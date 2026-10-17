using System.Numerics;
using Core.Geometry;
using Core.Objects;
using Core.Systems.Abstraction;

namespace Core.Objects.Frogs.Tongues;


public sealed class ShovelTongue(
    float length,
    float width,
    float slowSpeed,
    float strikeSpeed,
    float windupFraction = 0.35f,
    float returnSpeedMultiplier = 2.5f) : Tongue(length, width, slowSpeed)
{
    public override string Kind => "Shovel";

    /// <summary>Скорость резкого удара после фазы медленного подъёма.</summary>
    public float StrikeSpeed { get; } = MathF.Max(strikeSpeed, Constants.Epsilon);

    /// <summary>Доля длины, которую язык проходит медленно перед ударом (0..1).</summary>
    public float WindupFraction { get; } = Math.Clamp(windupFraction, 0.05f, 0.95f);

    /// <summary>Во время возврата язык убирается быстрее, чем поднимается.</summary>
    public float ReturnSpeedMultiplier { get; } = MathF.Max(returnSpeedMultiplier, 1f);

    public override bool Update(Frog owner, SimulationContext ctx)
    {
        var direction = owner.Facing;

        if (State == TongueState.Forwarding)
        {
            var currentLength = Tip.Length();
            var speed = currentLength < MaxLength * WindupFraction ? Speed : StrikeSpeed;
            var newLength = MathF.Min(currentLength + speed, MaxLength);
            Tip = direction * newLength;

            // Удар регистрируется ровно один раз — при достижении полной длины.
            if (newLength >= MaxLength - Constants.Epsilon)
            {
                Slap(owner, ctx, direction);
                State = TongueState.Returning;
            }

            return false;
        }

        if (State == TongueState.Returning)
        {
            var returnSpeed = Speed * ReturnSpeedMultiplier;
            var newLength = MathF.Max(0f, Tip.Length() - returnSpeed);
            Tip = direction * newLength;

            if (newLength > Constants.Epsilon)
                return false;

            EndAttack(owner, ctx);
            return true;
        }

        return false;
    }

    private void Slap(Frog owner, SimulationContext ctx, Vector2 direction)
    {
        var basePoint = owner.Position + owner.Radius * direction;
        var endPoint = basePoint + direction * MaxLength;
        var side = new Vector2(-direction.Y, direction.X) * (Width / 2f);
        var leftEnd = endPoint + side;
        var rightEnd = endPoint - side;

        foreach (var fly in ctx.Map.Query<Fly>())
        {
            if (fly.CaughtBy is not null)
                continue;

            if (!CircleIntersectsTriangle(fly.Position, fly.Radius, basePoint, leftEnd, rightEnd))
                continue;

            fly.CaughtBy = owner;
            Caught.Add(fly);
        }
    }

    private static bool CircleIntersectsTriangle(
        Vector2 center,
        float radius,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        if (IsInsideTriangle(center, a, b, c))
            return true;
        var distance = MathF.Min(
            Geometry.Geometry.DistanceToSegment(a, b, center),
            MathF.Min(
                Geometry.Geometry.DistanceToSegment(b, c, center),
                Geometry.Geometry.DistanceToSegment(c, a, center)));

        return distance <= radius;
    }

    private static bool IsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        var ab = Cross(b - a, point - a);
        var bc = Cross(c - b, point - b);
        var ca = Cross(a - c, point - c);

        var hasNegative = ab < 0f || bc < 0f || ca < 0f;
        var hasPositive = ab > 0f || bc > 0f || ca > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
