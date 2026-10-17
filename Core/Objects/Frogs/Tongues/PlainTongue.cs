using System.Numerics;
using Core.Systems.Abstraction;

namespace Core.Objects.Frogs.Tongues;

public class PlainTongue(float length, float speed) : Tongue(length, 4, speed)
{
    public override string Kind => "Plain";

    public override bool Update(Frog owner, SimulationContext ctx)
    {
        if (Caught.Count > 0 || Tip.Length() >= MaxLength - Constants.Epsilon)
            State = TongueState.Returning;

        if (State == TongueState.Forwarding)
        {
            foreach (var fly in ctx.Map.ObjectsInCircle<Fly>(owner.Position + Tip, Width / 2))
            {
                if (!Caught.Contains(fly))
                {
                    fly.CaughtBy = owner;
                    Caught.Add(fly);
                }
            }
        }

        var delta = Speed * (State == TongueState.Forwarding ? 1 : -1);
        var direction = Tip != Vector2.Zero ? Tip : owner.Facing;
        var newLength = Math.Clamp(Tip.Length() + delta, 0, MaxLength);
        Tip = Vector2.Normalize(direction) * newLength;

        owner.Aim(direction);

        if (newLength >= Constants.Epsilon) return false;

        EndAttack(owner, ctx);
        return true;
    }

}