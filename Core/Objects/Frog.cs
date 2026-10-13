using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.Objects;

[Flags]
public enum FrogState
{
    SittingOnWaterLily = 1,
    Swimming = 1<<1,
    Jumping = 1<<2,
    Dead = 1<<3,
    Attacking = 1<<4,
}

public class Frog : GameObject, IMovable, ICollidable, IControllable
{
    private const float Epsilon = 1e-6f;

    private const int JumpCooldownTicks = 60;
    public const int AttackCooldownTicks = 60;
    private const float JumpPeakHeight = 20f;
    private readonly Tongue _tongue;

    public Vector2 TonguePosition => Position + _tongue.RelativeTipPosition;
    public TongueState TongueState => _tongue.State;

    private float JumpSpeed { get; } = 1.5f;
    public float MaxJumpDistance { get; } = 100f;
    private float MaxWaterJumpDistance { get; } = 50f;
    private float SwimSpeed { get; } = 0.6f;


    public int NextJumpAvailableTick { get; private set; }
    public int NextAttackAvailableTick { get; private set; }

    public FrogState State { get; private set; } = FrogState.SittingOnWaterLily;
    public WaterLily? Lily { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.UnitY;
    public Vector2 Velocity { get; private set; }
    public Vector2 Drift { get; private set; } = Vector2.Zero;
    public int AirTicksRemaining { get; private set; }

    private int _jumpTotalTicks;

    public Frog(int id, Vector2 position, float radius, WaterLily? lily = null) : base(id, position, radius)
    {
        Lily = lily;
        _tongue = new Tongue(80, 3.5f);
    }

    public bool IsAlive => !State.HasFlag(FrogState.Dead);
    public bool IsInAir => State.HasFlag(FrogState.Jumping);
    public bool IsAvailable => (State & (FrogState.SittingOnWaterLily | FrogState.Swimming)) != 0;
    public bool IsAttacking => State.HasFlag(FrogState.Attacking);
    public IReadOnlyList<Fly> CaughtFlies => _tongue.CaughtFlies;

    /// <summary>Применяет смещение, посчитанное системой движения. Больше ничего не делает.</summary>
    public void MoveBy(Vector2 delta) => Position += delta;

    public void AddImpulse(Vector2 deltaV)
    {
        Drift += deltaV;
    }

    /// <summary>Смотреть в сторону, не двигаясь (прицел мышью). Нулевой вектор игнорируется.</summary>
    public void Aim(Vector2 direction)
    {
        if (IsAvailable && direction.LengthSquared() > Epsilon)
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
            || direction.LengthSquared() < Epsilon
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


    /// <summary>
    /// Вызывается системой раз в тик ПОСЛЕ движения: отсчитывает полёт и обновляет высоту по параболе.
    /// Возвращает true, когда прыжок закончился и нужно вызвать <see cref="Land"/>.
    /// </summary>
    public bool AdvanceJump()
    {
        if (!IsInAir) return false;

        AirTicksRemaining--;
        var t = 1f - (float)AirTicksRemaining / _jumpTotalTicks; // прогресс прыжка, 0..1
        Height = JumpPeakHeight * 4f * t * (1f - t); // 0 на старте и на финише, пик посередине
        return AirTicksRemaining <= 0;
    }

    /// <summary>Приземлилась на кувшинку или в воду (null).</summary>
    public void Land(WaterLily? lily)
    {
        StopMotion();
        Lily = lily;
        State = lily is null ? FrogState.Swimming : FrogState.SittingOnWaterLily;
    }

    /// <summary>Плыть в заданном направлении. Нулевой вектор останавливает лягушку.</summary>
    public void Swim(Vector2 direction)
    {
        if (State != FrogState.Swimming) return;

        Aim(direction);
        Velocity = direction.LengthSquared() > Epsilon ? Facing * SwimSpeed : Vector2.Zero;
    }

    public bool TryAttack(Vector2 direction, float distance, int tick)
    {
        var dist = Math.Clamp(distance, 0f, _tongue.MaxLength);

        if (!IsAvailable ||
            tick < NextAttackAvailableTick ||
            direction.LengthSquared() < Epsilon ||
            dist < 1e-3f) return false;
        
        Aim(direction);
        StopMotion();
        NextAttackAvailableTick = tick + AttackCooldownTicks;
        State |= FrogState.Attacking;
        _tongue.State = TongueState.Forwarding;
        return true;
    }

    public bool AdvanceAttack(Fly[] caughtFlies)
    {
        if (!IsAttacking) return false;
        foreach (var fly in caughtFlies)
            if (!_tongue.CaughtFlies.Contains(fly))
                _tongue.CaughtFlies.Add(fly);

        if (_tongue.CaughtFlies.Count > 0 || _tongue.RelativeTipPosition.Length() >= _tongue.MaxLength - Epsilon)
            _tongue.State = TongueState.Returning;

        var delta = _tongue.Speed * (_tongue.State == TongueState.Forwarding ? 1 : -1);
        var direction = _tongue.RelativeTipPosition != Vector2.Zero ? _tongue.RelativeTipPosition : Facing;
        var newTongueLength = Math.Clamp(_tongue.RelativeTipPosition.Length() + delta, 0, _tongue.MaxLength);

        _tongue.RelativeTipPosition = Vector2.Normalize(direction) * newTongueLength;
        
        
        Aim(direction);

        return newTongueLength < Epsilon;
    }

    public void EndAttack()
    {
        State &= ~FrogState.Attacking;
        _tongue.RelativeTipPosition = Vector2.Zero;
        _tongue.State = TongueState.InMouth;
        _tongue.CaughtFlies.Clear();
    }

    public void Die()
    {
        State |= FrogState.Dead;
        StopMotion();
    }

    /// <summary>Респавн на кувшинке (для раундов): сбрасывает состояние и кулдауны.</summary>
    public void Respawn(WaterLily lily)
    {
        StopMotion();
        Position = lily.Position;
        Lily = lily;
        State = FrogState.SittingOnWaterLily;
        NextJumpAvailableTick = 0;
        NextAttackAvailableTick = 0;
    }

    private void StopMotion()
    {
        Velocity = Vector2.Zero;
        AirTicksRemaining = 0;
        Height = 0;
    }

    public bool CollidesWith(ICollidable other)
    {
        if (other is WaterLily && (State & (FrogState.SittingOnWaterLily | FrogState.Jumping)) != 0) return false;
        return true;
    }
}

public enum TongueState
{
    Forwarding,
    Returning,
    InMouth,
}

public class Tongue(float length, float speed)
{
    public float MaxLength { get; set; } = length;
    public float Speed { get; set; } = speed;
    public Vector2 RelativeTipPosition { get; set; } = Vector2.Zero;
    public List<Fly> CaughtFlies { get; } = [];
    public TongueState State { get; set; } = TongueState.InMouth;
}