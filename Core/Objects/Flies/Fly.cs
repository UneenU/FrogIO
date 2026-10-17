using System.Numerics;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;

namespace Core.Objects;

public abstract class Fly : GameObject, IMovable, IControllable
{
    public string Name { get; }
    public float Mass { get; }
    public float FlySpeed { get; }
    public int Points { get; }

    public Vector2 Facing { get; private set; } = Vector2.UnitY;
    public Vector2 Velocity { get; private set; }
    public Vector2 Drift { get; private set; } = Vector2.Zero;
    
    public Frog? CaughtBy { get; set; }

    protected Fly(Vector2 position, FlyConfig config) : base(position, config.Radius)
    {
        Name = config.Name;
        Mass = config.Mass;
        Points = config.Points;
        FlySpeed = config.FlySpeed;
        Height = FlyConfig.FlyHeight;
    }

    public void MoveBy(Vector2 delta) => Position += delta;

    public void AddImpulse(Vector2 deltaV)
    {
        Drift += deltaV;
    }

    public virtual void Steer(Vector2 direction)
    {
        if (CaughtBy is not null ||
            direction.LengthSquared() < Constants.Epsilon)
        {
            Velocity = Vector2.Zero;
            return;
        }
        
        Facing = Vector2.Normalize(direction);
        Velocity = Facing * FlySpeed;
    }
}