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
    /// <summary>
    /// Сведения о языке для сериализации состояния игры и отрисовки клиентом.
    /// Phase — нормализованная текущая длина языка (0..1), Param1 — ширина,
    /// Param2 — максимальная длина.
    /// </summary>
    public virtual TongueInfo ToInfo(Frog owner)
    {
        var tip = GetTip(owner);
        var phase = MaxLength > Constants.Epsilon
            ? Math.Clamp(Tip.Length() / MaxLength, 0f, 1f)
            : 0f;
        return new TongueInfo(Kind, tip.X, tip.Y, phase, Width);
    }

    public virtual Vector2 GetTip(Frog owner)
    {
        return owner.Position + owner.Radius*owner.Facing + Tip;
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
    float Width);