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
    public TongueState State { get; protected set; } = TongueState.InMouth;

    public abstract bool Update(Frog owner, SimulationContext ctx);
    public abstract TongueInfo ToInfo(Frog owner);

    public virtual Vector2 GetTip(Frog owner)
    {
        return owner.Position + Tip;
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
    float TipX,
    float TipY,
    float Phase,
    float Param1 = 0,
    float Param2 = 0);