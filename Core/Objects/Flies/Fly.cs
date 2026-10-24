using System.Numerics;
using Core.Commands;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;

namespace Core.Objects;

public abstract class Fly : GameObject, IMovable, IControllable
{
    public string Name { get; }
    public float Mass { get; }
    public int ScorePoints { get; }
    public int XpForEating { get; } = 5000;
    public Effect? Effect { get; set; }
    public Vector2 Facing { get; private set; } = Vector2.UnitY;

    public float FlySpeed { get; }
    public Vector2 Velocity { get; private set; }
    public Vector2 Drift { get; private set; } = Vector2.Zero;

    public Frog? CaughtBy { get; set; }

    protected Fly(Vector2 position, FlyConfig config, Effect? effect = null) : base(position, config.Radius)
    {
        Name = config.Name;
        Mass = config.Mass;
        ScorePoints = config.Points;
        FlySpeed = config.FlySpeed;
        Height = FlyConfig.FlyHeight;
        Effect = effect;
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

    public void MoveBy(Vector2 delta) => Position += delta;

    public void AddImpulse(Vector2 deltaV)
    {
        Drift += deltaV;
    }
}