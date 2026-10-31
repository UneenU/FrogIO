using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.Objects;

public class WaterLily(Vector2 position, float radius) : GameObject(position, radius), IMovable, ICollidable, IFloating
{
    public bool IsAfloat => true;

    public Vector2 Velocity { get; } = Vector2.Zero;
    public float Mass { get; } = 200;
    public Vector2 Drift { get; private set; } = Vector2.Zero;

    public void MoveBy(Vector2 delta)
    {
        Position += delta;
    }

    public void AddImpulse(Vector2 deltaV)
    {
        Drift += deltaV;
    }

    public bool Contains(Vector2 frogPosition)
    {
        if ((frogPosition - Position).Length() > Radius) return false;
        return true;
    }
}