using System.Numerics;
using Core.Systems.Abstraction;

namespace Core.Objects.Frogs.Tongues;

public sealed class SniperTongue(TongueConfig config) : Tongue(config)
{
    private Vector2 _previousTip;

    public override string Kind => "Sniper";

    protected override bool IntersectsCircle(Frog owner, Vector2 center, float radius) =>
        Geometry.Geometry.CircleIntersectsCapsule(center, radius, _previousTip, GetTip(owner), Width / 2f);

    public override bool Update(Frog owner, SimulationContext ctx)
    {
        if (State == TongueState.InMouth) return false;

        _previousTip = GetTip(owner);
        var direction = Tip != Vector2.Zero ? Vector2.Normalize(Tip) : owner.Facing;
        var delta = Speed * (State == TongueState.Forwarding ? 1 : -1);
        var newLength = Math.Clamp(Tip.Length() + delta, 0f, MaxLength);
        Tip = direction * newLength;

        if (State == TongueState.Forwarding)
        {
            if (!TryHitFrog(owner, ctx))
            {
                // Проверяем путь кончика за тик, чтобы быстрый язык не перескакивал через мух.
                foreach (var fly in ctx.Map.Query<Fly>())
                {
                    if (fly.CaughtBy is not null || !IntersectsCircle(owner, fly.Position, fly.Radius))
                        continue;

                    fly.CaughtBy = owner;
                    Caught.Add(fly);
                }
            }

            if (newLength >= MaxLength - Constants.Epsilon)
                State = TongueState.Returning;
        }

        if (newLength >= Constants.Epsilon) return false;

        EndAttack(owner, ctx);
        return true;
    }
}
