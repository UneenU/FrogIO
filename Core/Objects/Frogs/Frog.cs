using System.Numerics;
using Core.Commands;
using Core.Objects.Abstraction;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

[Flags]
public enum FrogState
{
    SittingOnWaterLily = 1,
    Jumping = 1 << 1,
    Swimming = 1 << 2,
    Dead = 1 << 3,
    Attacking = 1 << 4,
}

public enum TongueState
{
    InMouth = 0,
    Forwarding = 1,
    Returning = 2,
}

public abstract class Frog : GameObject, IMovable, ICollidable, IControllable
{
    public readonly Tongue Tongue;
    private int _jumpTotalTicks;
    private Vector2? _targetToMove;
    private WaterLily? _lilyToReach = null;

    private readonly List<TimedEffect> _activeTimedEffects = new();
    private readonly Dictionary<string, StatModifiers> _modifiersByEffect = new();
    public IReadOnlyList<TimedEffect> ActiveTimedEffects => _activeTimedEffects;
    public bool HasEffect(string id) => _activeTimedEffects.Any(e => e.Id == id);

    private float GetModifier(Func<StatModifiers, float> selector)
    {
        var modifier = 1f;
        foreach (var mods in _modifiersByEffect.Values)
            modifier *= selector(mods);
        return modifier;
    }


    public string Name { get; }
    public float RadiusPerMass { get; }

    public float JumpSpeed
    {
        get => field * GetModifier(m => m.JumpSpeedMul);
        set => field = value;
    }

    public float MaxJumpDistance
    {
        get => field * GetModifier(m => m.MaxJumpDistanceMul);
        set => field = value;
    }

    public float SwimSpeed
    {
        get => field * GetModifier(m => m.SwimSpeedMul);
        set => field = value;
    }

    public float MaxWaterJumpDistance { get; }
    public int JumpCooldownTicks { get; protected internal set; }
    public float JumpPeakHeight { get; }

    public float Hp { get; set; } // todo
    public int Level { get; internal set; } = 1;
    public int Xp { get; private set; } = 0;
    public float Mass { get; protected internal set; }

    public FrogState State { get; private set; } = FrogState.SittingOnWaterLily;
    public WaterLily? Lily { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.UnitY;
    public Vector2 Velocity { get; private set; }
    public Vector2 Drift { get; private set; } = Vector2.Zero;
    public int AirTicksRemaining { get; private set; }
    public int NextJumpAvailableTick { get; private set; }
    public int NextAttackAvailableTick { get; private set; }

    // -------- Конструктор --------
    protected Frog(Vector2 position, FrogConfig config, WaterLily? lily = null, Tongue? tongue = null)
        : base(position, config.Radius)
    {
        Name = config.Name;
        Radius = config.Radius;
        RadiusPerMass = config.RadiusPerMass;
        JumpSpeed = config.JumpSpeed;
        MaxJumpDistance = config.MaxJumpDistance;
        MaxWaterJumpDistance = config.MaxWaterJumpDistance;
        JumpPeakHeight = config.JumpPeakHeight;
        JumpCooldownTicks = config.JumpCooldownTicks;
        SwimSpeed = config.SwimSpeed;

        Hp = config.Hp;
        Mass = config.Mass;
        Lily = lily;
        Tongue = tongue ?? new PlainTongue(config.TongueLength, config.TongueSpeed);
    }

    // -------- Производные свойства --------
    public Vector2 TonguePosition => Tongue.GetTip(this);
    public TongueState TongueState => Tongue.State;

    public bool IsAlive => !State.HasFlag(FrogState.Dead);
    public bool IsInAir => State.HasFlag(FrogState.Jumping);
    public bool IsInWater => State.HasFlag(FrogState.Swimming);
    public bool IsAvailable => (State & (FrogState.Attacking | FrogState.Jumping | FrogState.Dead)) == 0;
    public bool IsAttacking => State.HasFlag(FrogState.Attacking);

    // -------- Публичный API --------
    /// <summary>Применяет смещение, посчитанное системой движения. Больше ничего не делает.</summary>
    public void MoveBy(Vector2 delta) => Position += delta;

    public void AddImpulse(Vector2 deltaV) => Drift += deltaV;

    /// <summary>Смотреть в сторону, не двигаясь (прицел мышью). Нулевой вектор игнорируется.</summary>
    public void Aim(Vector2 direction)
    {
        if (IsAvailable && direction.LengthSquared() > Constants.Epsilon)
            Facing = Vector2.Normalize(direction);
    }

    public bool TryStartJump(Vector2 direction, float distance, int tick, bool lilyAtTarget)
    {
        var dist = State switch
        {
            FrogState.SittingOnWaterLily => Math.Clamp(distance, 0f, MaxJumpDistance),
            FrogState.Swimming when lilyAtTarget && distance <= MaxWaterJumpDistance => distance,
            _ => 0f
        };

        if (tick < NextJumpAvailableTick
            || direction.LengthSquared() < Constants.Epsilon
            || dist < 1e-3f)
            return false;

        Aim(direction);
        _jumpTotalTicks = Math.Max(1, (int)MathF.Ceiling(dist / JumpSpeed));
        AirTicksRemaining = _jumpTotalTicks;
        Velocity = Facing * (dist / _jumpTotalTicks);

        NextJumpAvailableTick = tick + JumpCooldownTicks;
        Lily = null;
        State = FrogState.Jumping;
        return true;
    }

    public bool AdvanceJump()
    {
        if (!IsInAir) return false;

        AirTicksRemaining--;
        var t = 1f - (float)AirTicksRemaining / _jumpTotalTicks; // прогресс прыжка, 0..1
        Height = JumpPeakHeight * 4f * t * (1f - t); // пик посередине
        return AirTicksRemaining <= 0;
    }

    public void Land(WaterLily? lily)
    {
        StopMotion();
        AirTicksRemaining = 0;
        Height = 0;
        Lily = lily;
        State = lily is null ? FrogState.Swimming : FrogState.SittingOnWaterLily;
    }

    public void Swim(Vector2 target)
    {
        if (State != FrogState.Swimming) return;
        var direction = target - Position;
        var dist = direction.Length() - Radius;
        if (dist < Constants.Epsilon)
        {
            StopMotion();
            return;
        }

        Aim(direction);
        Velocity = Facing * Math.Min(SwimSpeed, dist);
        if (dist < SwimSpeed) _targetToMove = null;
    }

    public bool TryAttack(Vector2 direction, float distance, int tick)
    {
        if (!IsAvailable ||
            tick < NextAttackAvailableTick ||
            direction.LengthSquared() < Constants.Epsilon) return false;

        Aim(direction);
        StopMotion();
        NextAttackAvailableTick = tick + Tongue.CooldownTicks;
        State |= FrogState.Attacking;
        Tongue.Start(this);
        return true;
    }

    public void EndAttack()
    {
        State &= ~FrogState.Attacking;
    }

    public void Eat(Fly fly)
    {
        Mass += fly.Mass;
        Radius += fly.Mass * 0.01f;
    }

    public bool ApplyEffect(Effect effect)
    {
        if (!IsAlive) return false;
        if (HasEffect(effect.Id)) return false;
        if (!effect.Apply(this)) return false;

        if (effect is TimedEffect timedEffect)
            _activeTimedEffects.Add(timedEffect);
        return true;
    }

    public void RemoveEffect(TimedEffect effect)
    {
        if (_activeTimedEffects.Remove(effect))
        {
            effect.Remove(this);
        }
    }

    internal void SetModifiers(string effectId, StatModifiers mods)
    {
        _modifiersByEffect[effectId] = mods;
    }

    internal void ClearModifiers(string effectId)
    {
        _modifiersByEffect.Remove(effectId);
    }

    public void AddXp(float amount)
    {
        if (amount <= 0f || !IsAlive) return;
        Xp += (int)(amount * GetModifier(m => m.XpGainMul));
    }

    public void Die()
    {
        StopMotion();
        Tongue.Cancel();
        EndAttack();
        State |= FrogState.Dead;
        State &= ~FrogState.Jumping;
    }

    public void Respawn(WaterLily lily)
    {
        StopMotion();
        Position = lily.Position;
        Lily = lily;
        State = FrogState.SittingOnWaterLily;
        NextJumpAvailableTick = 0;
        NextAttackAvailableTick = 0;
    }

    public bool CollidesWith(ICollidable other)
    {
        if (other is WaterLily
            && (State & (FrogState.SittingOnWaterLily | FrogState.Jumping)) != 0)
            return false;

        return true;
    }

    private void StopMotion()
    {
        Velocity = Vector2.Zero;
        _targetToMove = null;
        _lilyToReach = null;
    }

    public void SetTargetToMove(Vector2 targetToMove)
    {
        _lilyToReach = null;
        _targetToMove = targetToMove;
    }

    public void SetTargetToMove(GameObject target)
    {
        _targetToMove = null;
        if (target is WaterLily lily)
            _lilyToReach = lily;
    }

    public void MoveToTarget(int tick)
    {
        if (_lilyToReach is not null)
        {
            var dir = _lilyToReach.Position - Position;
            var jumped = TryStartJump(dir, dir.Length(), tick, true);
            if (!jumped) Swim(_lilyToReach.Position);
        }
        else if (_targetToMove is not null)
        {
            var dir = _targetToMove.Value - Position;
            if (State.HasFlag(FrogState.SittingOnWaterLily) &&
                TryStartJump(dir, dir.Length(), tick, false)) return;
            Swim(_targetToMove.Value);
        }
        else
        {
            StopMotion();
        }
    }
}