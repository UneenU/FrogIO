using System.Numerics;
using Core.Events;

namespace Core;

public enum FrogState
{
    SittingOnWaterLily,
    Swimming,
    Jumping,
    Dead,
}

public class Frog(int id, Vector2 position, float radius, WaterLily? lily = null)
    : GameObject(id, position, radius), IMovable
{
    private const float Epsilon = 1e-6f;

    private const int JumpCooldownTicks = 60;
    public const int AttackCooldownTicks = 120;
    private const float JumpPeakHeight = 1.5f;

    private float JumpSpeed { get; } = 0.25f;
    private float MaxJumpDistance { get; } = 4f;
    private float SwimSpeed { get; } = 0.1f;
    public float TongueRange { get; } = 2.5f;

    public int NextJumpAvailableTick { get; private set; }
    public int NextAttackAvailableTick { get; private set; }

    public FrogState State { get; private set; } = FrogState.SittingOnWaterLily;
    public WaterLily? Lily { get; private set; } = lily;
    public Vector2 Facing { get; private set; } = Vector2.UnitY;
    public Vector2 Velocity { get; private set; }
    public int AirTicksRemaining { get; private set; }

    private int _jumpTotalTicks;

    public bool IsAlive => State != FrogState.Dead;
    public bool IsInAir => State == FrogState.Jumping;

    /// <summary>Применяет смещение, посчитанное системой движения. Больше ничего не делает.</summary>
    public void MoveBy(Vector2 delta) => Position += delta;

    /// <summary>Смотреть в сторону, не двигаясь (прицел мышью). Нулевой вектор игнорируется.</summary>
    public void Aim(Vector2 direction)
    {
        if (IsAlive && direction.LengthSquared() > Epsilon)
            Facing = Vector2.Normalize(direction);
    }

    /// <summary>
    /// Пытается начать прыжок. Направление нормализуется, дальность ограничивается.
    /// Возвращает false, если нельзя (системе тогда НЕ нужно создавать событие).
    /// </summary>
    public bool TryStartJump(Vector2 direction, float distance, int tick)
    {
        var dist = Math.Clamp(distance, 0f, MaxJumpDistance);
        if (State != FrogState.SittingOnWaterLily
            || tick < NextJumpAvailableTick
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
        Height = JumpPeakHeight * 4f * t * (1f - t);             // 0 на старте и на финише, пик посередине
        return AirTicksRemaining <= 0;
    }

    /// <summary>Приземлилась на кувшинку или в воду (null).</summary>
    public GameEvent Land(WaterLily? lily)
    {
        StopMotion();
        Lily = lily;

        if (lily is null)
        {
            State = FrogState.Swimming;
            return new FrogJumpedInWaterEvent(this);
        }

        State = FrogState.SittingOnWaterLily;
        return new FrogLandOnWaterLilyEvent(this, lily);
    }

    /// <summary>Плыть в заданном направлении. Нулевой вектор останавливает лягушку.</summary>
    public void Swim(Vector2 direction)
    {
        if (State != FrogState.Swimming) return;

        Aim(direction);
        Velocity = direction.LengthSquared() > Epsilon ? Facing * SwimSpeed : Vector2.Zero;
    }

    public void Die()
    {
        State = FrogState.Dead;
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
}