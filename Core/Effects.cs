using Core.Objects.Frogs;

namespace Core;

public sealed record StatModifiers
{
    public static readonly StatModifiers None = new();

    public float JumpSpeedMul         { get; init; } = 1f;
    public float MaxJumpDistanceMul   { get; init; } = 1f;
    public float SwimSpeedMul         { get; init; } = 1f;
    public float TongueLengthMul      { get; init; } = 1f;
    public float XpGainMul            { get; init; } = 1f;
}

public abstract class Effect
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }

    public abstract bool Apply(Frog frog);
    public abstract void Remove(Frog frog);
}

public abstract class InstantEffect : Effect
{
    public sealed override void Remove(Frog frog) { }
}

public abstract class TimedEffect : Effect
{
    public float Duration  { get; }
    public float Elapsed   { get; protected set; }
    public float Remaining => Math.Max(0f, Duration - Elapsed);
    public bool  IsExpired => Elapsed >= Duration;

    protected TimedEffect(float duration)
    {
        if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
        Duration = duration;
    }

    public void Tick() => Elapsed++;

    public override void Remove(Frog frog) => frog.ClearModifiers(Id);
}

public sealed class InstantXpEffect(float amount) : InstantEffect
{
    public override string Id => "instant_xp";
    public override string DisplayName => $"Опыт +{Amount}";

    private float Amount { get; } = amount;

    public override bool Apply(Frog frog) { frog.AddXp(Amount); return true; }
}

public sealed class SpeedBoostEffect(float duration, float factor = 1.5f) : TimedEffect(duration)
{
    public override string Id => "speed_boost";
    public override string DisplayName => "Ускорение";

    public float Factor { get; } = factor;

    public override bool Apply(Frog frog)
    {
        frog.SetModifiers(Id, new StatModifiers
        {
            JumpSpeedMul       = Factor,
            SwimSpeedMul       = Factor,
            MaxJumpDistanceMul = 1.2f,
        });
        return true;
    }
}


public sealed class XpGainBoostEffect(float duration, float factor = 2f) : TimedEffect(duration)
{
    public override string Id => "xp_boost";
    public override string DisplayName => "Прозрение";

    private float Factor { get; } = factor;

    public override bool Apply(Frog frog)
    {
        frog.SetModifiers(Id, new StatModifiers { XpGainMul = Factor });
        return true;
    }
}