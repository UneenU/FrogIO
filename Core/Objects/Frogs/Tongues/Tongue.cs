using System.Numerics;
using Core.Events;
using Core.Systems.Abstraction;

namespace Core.Objects.Frogs.Tongues;

public abstract class Tongue(float length, float width, float speed)
{
    public abstract string Kind { get; }

    protected readonly List<Fly> Caught = [];
    protected Vector2 Tip = Vector2.Zero;

    public virtual int CooldownTicks => 60;
    public float MaxLength { get; set; } = length;
    public float Width { get; set; } = width;
    public float Speed { get; set; } = speed;
    public float HitImpulse { get; set; } = 1200f;
    public TongueState State { get; protected set; } = TongueState.InMouth;

    public Tongue(TongueConfig config) : this(config.Length, config.Width, config.Speed)
    {
    }

    public abstract bool Update(Frog owner, SimulationContext ctx);

    protected virtual bool IntersectsCircle(Frog owner, Vector2 center, float radius) =>
        Geometry.Geometry.CirclesIntersect(GetTip(owner), Width / 2f, center, radius);

    protected bool TryHitFrog(Frog owner, SimulationContext ctx)
    {
        if (State != TongueState.Forwarding || Tip.LengthSquared() < Constants.Epsilon)
            return false;

        Frog? target = null;
        var nearestDistance = float.PositiveInfinity;
        foreach (var frog in ctx.Map.Query<Frog>())
        {
            if (ReferenceEquals(frog, owner) || !frog.IsAlive ||
                !IntersectsCircle(owner, frog.Position, frog.Radius)) continue;

            var distance = Vector2.DistanceSquared(owner.Position, frog.Position);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            target = frog;
        }

        if (target is null) return false;

        var offset = target.Position - owner.Position;
        var direction = offset.LengthSquared() > Constants.Epsilon
            ? Vector2.Normalize(offset)
            : owner.Facing;
        owner.AddImpulse(direction * (HitImpulse / owner.Mass));
        target.AddImpulse(-direction * (HitImpulse / target.Mass));
        State = TongueState.Returning;
        return true;
    }

    public virtual TongueInfo ToInfo(Frog owner)
    {
        var tip = GetTip(owner);
        var phase = MaxLength > Constants.Epsilon
            ? Math.Clamp(Tip.Length() / MaxLength, 0f, 1f)
            : 0f;
        return new TongueInfo(Kind, State, tip.X, tip.Y, phase, Width);
    }

    public virtual Vector2 GetTip(Frog owner)
    {
        return owner.Position + owner.Radius * owner.Facing + Tip;
    }

    public virtual void EndAttack(Frog owner, SimulationContext ctx)
    {
        foreach (var fly in Caught)
        {
            owner.Eat(fly);
            ctx.Events.Publish(new FrogEatFlyEvent(owner, fly));
        }

        Reset();
    }

    public virtual void Cancel()
    {
        foreach (var fly in Caught)
            fly.CaughtBy = null;

        Reset();
    }

    protected virtual void Reset()
    {
        Caught.Clear();
        Tip = Vector2.Zero;
        State = TongueState.InMouth;
    }

    public void Start(Frog owner)
    {
        State = TongueState.Forwarding;
    }
}

public record TongueInfo(
    string Kind,
    TongueState State,
    float TipX,
    float TipY,
    float Phase,
    float Width);

public record TongueConfig(float Length, float Width, float Speed);
