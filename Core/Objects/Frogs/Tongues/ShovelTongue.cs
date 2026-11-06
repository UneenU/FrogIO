using System.Numerics;
using Core.Systems.Abstraction;

namespace Core.Objects.Frogs.Tongues;


public sealed class ShovelTongue(TongueConfig config) : Tongue(config)
{
    public override string Kind => "Shovel";

    public override bool Update(Frog owner, SimulationContext ctx)
    {
        if (State == TongueState.InMouth) return false;

        var delta = Speed * (State == TongueState.Forwarding ? 1 : -1);
        var direction = Tip != Vector2.Zero ? Vector2.Normalize(Tip) : owner.Facing;
        var newLength = Math.Clamp(Tip.Length() + delta, 0f, MaxLength);
        Tip = direction * newLength;

        owner.Aim(direction);

        if (State == TongueState.Forwarding)
        {
            if (!TryHitFrog(owner, ctx))
                CatchFlies(owner, ctx);
            if (newLength >= MaxLength - Constants.Epsilon)
                State = TongueState.Returning;
        }

        if (newLength >= Constants.Epsilon) return false;

        EndAttack(owner, ctx);
        return true;
    }

    private void CatchFlies(Frog owner, SimulationContext ctx)
    {
        if (Tip.LengthSquared() < Constants.Epsilon) return;

        foreach (var fly in ctx.Map.Query<Fly>())
        {
            if (fly.CaughtBy is not null || !IntersectsCircle(owner, fly.Position, fly.Radius))
                continue;

            fly.CaughtBy = owner;
            Caught.Add(fly);
        }
    }

    protected override bool IntersectsCircle(Frog owner, Vector2 center, float radius)
    {
        var direction = Vector2.Normalize(Tip);
        var basePoint = owner.Position + owner.Radius * direction;
        var endPoint = basePoint + Tip;
        var side = new Vector2(-direction.Y, direction.X) * (Width / 2f);
        var leftEnd = endPoint + side;
        var rightEnd = endPoint - side;

        return Geometry.Geometry.CircleIntersectsTriangle(center, radius, basePoint, leftEnd, rightEnd);
    }
}
