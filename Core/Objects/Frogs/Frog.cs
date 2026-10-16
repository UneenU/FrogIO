using System.Numerics;
using Core.Objects.Abstraction;

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
    protected readonly Tongue _tongue;
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
    public int AttackCooldownTicks { get; }

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
    protected Frog(Vector2 position, FrogConfig config, WaterLily? lily = null)
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
        AttackCooldownTicks = config.AttackCooldownTicks;

        // Стартовое состояние
        Mass = config.Mass;
        Lily = lily;
        _tongue = new Tongue(config.TongueLength, config.TongueSpeed);
    }

    // -------- Производные свойства --------
    public Vector2 TonguePosition => Position + _tongue.RelativeTipPosition;
    public TongueState TongueState => _tongue.State;
    public IReadOnlyList<Fly> CaughtFlies => _tongue.CaughtFlies;

    public bool IsAlive => !State.HasFlag(FrogState.Dead);
    public bool IsInAir => State.HasFlag(FrogState.Jumping);
    public bool IsAvailable => (State & (FrogState.Attacking | FrogState.Jumping | FrogState.Dead) ) == 0;
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
        var dist = Math.Clamp(distance, 0f, _tongue.MaxLength);

        if (!IsAvailable
            || tick < NextAttackAvailableTick
            || direction.LengthSquared() < Constants.Epsilon
            || dist < 1e-3f)
            return false;

        Aim(direction);
        StopMotion();

        NextAttackAvailableTick = tick + AttackCooldownTicks;
        State |= FrogState.Attacking;
        _tongue.State = TongueState.Forwarding;
        return true;
    }

    public bool AdvanceAttack(List<GameObject> caughtObjects)
    {
        if (!IsAttacking) return false;

        foreach (var obj in caughtObjects)
        {
            if (obj is not Fly fly)
            {
                _tongue.State = TongueState.Returning;
                continue;
            }
            
            if (_tongue.State != TongueState.Forwarding) continue;
            if (!_tongue.CaughtFlies.Contains(fly))
                _tongue.CaughtFlies.Add(fly);
        }

        if (_tongue.CaughtFlies.Count > 0 ||
            _tongue.RelativeTipPosition.Length() >= _tongue.MaxLength - Constants.Epsilon)
            _tongue.State = TongueState.Returning;

        var delta = _tongue.Speed * (_tongue.State == TongueState.Forwarding ? 1 : -1);
        var direction = _tongue.RelativeTipPosition != Vector2.Zero
            ? _tongue.RelativeTipPosition
            : Facing;
        var newTongueLen = Math.Clamp(_tongue.RelativeTipPosition.Length() + delta, 0, _tongue.MaxLength);

        _tongue.RelativeTipPosition = Vector2.Normalize(direction) * newTongueLen;

        Aim(direction);

        return newTongueLen < Constants.Epsilon;
    }

    public void EndAttack()
    {
        State &= ~FrogState.Attacking;
        _tongue.RelativeTipPosition = Vector2.Zero;
        _tongue.State = TongueState.InMouth;

        EatFlies();
    }

    public void Die()
    {
        State = FrogState.Dead;
        StopMotion();
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

    protected virtual void EatFlies()
    {
        foreach (var fly in _tongue.CaughtFlies)
        {
            Mass += fly.Mass;
            Radius += fly.Mass * RadiusPerMass;
        }

        _tongue.CaughtFlies.Clear();
    }

    private void StopMotion()
    {
        Velocity = Vector2.Zero;
        AirTicksRemaining = 0;
        Height = 0;
    }
}

public sealed class CommonFrog(
    Vector2 position,
    WaterLily? lily = null)
    : Frog(position, FrogPresets.Common, lily);

public sealed class HeavyFrog(
    Vector2 position,
    WaterLily? lily = null)
    : Frog(position, FrogPresets.Heavy, lily);

public class Tongue(float length, float speed)
{
    public float MaxLength { get; set; } = length;
    public float Speed { get; set; } = speed;
    public Vector2 RelativeTipPosition { get; set; } = Vector2.Zero;
    public List<Fly> CaughtFlies { get; } = [];
    public TongueState State { get; set; } = TongueState.InMouth;
}