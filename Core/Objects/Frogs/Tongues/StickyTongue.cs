using System.Numerics;
using Core.Systems.Abstraction;

namespace Core.Objects.Frogs.Tongues;

public class StickyTongue(TongueConfig config) : Tongue(config)
{
    public override string Kind => "sticky";

    protected override bool IntersectsCircle(Frog owner, Vector2 center, float radius) =>
        Geometry.Geometry.CircleIntersectsCapsule(center, radius,
            owner.Position + owner.Radius * owner.Facing, GetTip(owner), Width / 2f);

    public override bool Update(Frog owner, SimulationContext ctx)
    {
        if (Tip.Length() >= MaxLength - Constants.Epsilon)
            State = TongueState.Returning;

        var searchCircleCenter = owner.Position + Tip / 2;
        var searchCircleRadius = Tip.Length() / 2;
        foreach (var fly in ctx.Map.ObjectsInCircle<Fly>(searchCircleCenter, searchCircleRadius))
        {
            if (!Geometry.Geometry.CircleIntersectsCapsule(
                    fly.Position, fly.Radius, owner.Position, GetTip(owner), Width / 2f)) continue;

            fly.CaughtBy = owner;
            if (!Caught.Contains(fly))
                Caught.Add(fly);
        }

        var delta = Speed * (State == TongueState.Forwarding ? 1 : -1);
        var direction = Tip != Vector2.Zero ? Tip : owner.Facing;
        var newLength = Math.Clamp(Tip.Length() + delta, 0, MaxLength);
        Tip = Vector2.Normalize(direction) * newLength;

        owner.Aim(direction);

        TryHitFrog(owner, ctx);

        if (newLength >= Constants.Epsilon) return false;

        EndAttack(owner, ctx);
        return true;
    }

}
