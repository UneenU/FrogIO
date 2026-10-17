using System.Numerics;
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
    // -------- Поля --------
    public readonly Tongue Tongue;
    private int _jumpTotalTicks;

    // -------- Характеристики вида --------
    public string Name { get; }
    public float RadiusPerMass { get; }
    public float JumpSpeed { get; }
    public float MaxJumpDistance { get; }
    public float MaxWaterJumpDistance { get; }
    public float JumpPeakHeight { get; }
    public int JumpCooldownTicks { get; }
    public float SwimSpeed { get; }

    // -------- Изменяемое состояние --------
    public float Mass { get; protected set; }

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
        // Характеристики вида
        Name = config.Name;
        Radius = config.Radius;
        RadiusPerMass = config.RadiusPerMass;
        JumpSpeed = config.JumpSpeed;
        MaxJumpDistance = config.MaxJumpDistance;
        MaxWaterJumpDistance = config.MaxWaterJumpDistance;
        JumpPeakHeight = config.JumpPeakHeight;
        JumpCooldownTicks = config.JumpCooldownTicks;
        SwimSpeed = config.SwimSpeed;

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
            FrogState.SittingOnWaterLily
                => Math.Clamp(distance, 0f, MaxJumpDistance),

            FrogState.Swimming when lilyAtTarget
                                    && distance <= MaxWaterJumpDistance
                => distance,

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
        Lily = lily;
        State = lily is null ? FrogState.Swimming : FrogState.SittingOnWaterLily;
    }

    public void Swim(Vector2 direction)
    {
        if (State != FrogState.Swimming) return;

        Aim(direction);
        Velocity = direction.LengthSquared() > Constants.Epsilon
            ? Facing * SwimSpeed
            : Vector2.Zero;
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

    public void StopMotion()
    {
        Velocity = Vector2.Zero;
        AirTicksRemaining = 0;
        Height = 0;
    }
}