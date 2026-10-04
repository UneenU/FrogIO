using System.Numerics;

namespace Core;

public sealed class Fly : GameObject, IMovable
{
    private const float Epsilon = 1e-6f;

    public const float FlyHeight = 2f;

    public float FlySpeed { get; } = 0.15f;

    public Vector2 Facing { get; private set; } = Vector2.UnitY;
    public Vector2 Velocity { get; private set; }
    public Vector2 Drift { get; private set; } = Vector2.Zero;

    public Fly(int id, Vector2 position, float radius) : base(id, position, radius)
    {
        Height = FlyHeight;
    }

    /// <summary>Применяет смещение, посчитанное системой движения. Больше ничего не делает.</summary>
    public void MoveBy(Vector2 delta) => Position += delta;

    public void AddImpulse(Vector2 deltaV)
    {
        Drift += deltaV;
    }

    /// <summary>Лететь в заданном направлении. Нулевой вектор останавливает муху (зависает на месте).</summary>
    public void Steer(Vector2 direction)
    {
        if (direction.LengthSquared() < Epsilon)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Facing = Vector2.Normalize(direction);
        Velocity = Facing * FlySpeed;
    }
}