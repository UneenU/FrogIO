using System.Numerics;

namespace Core.Objects.Abstraction;

public interface IMovable
{
    Vector2 Velocity { get; }
    Vector2 Position { get; }
    float Mass { get; }

    Vector2 Drift { get; }
    sealed Vector2 TotalVelocity => Velocity + Drift;
    void MoveBy(Vector2 delta);
    void AddImpulse(Vector2 deltaV);

    void DampDrift(float delta)
    {
        if (Drift == Vector2.Zero) return;
        if (delta > Drift.Length()) delta = Drift.Length();
        AddImpulse(-delta * Vector2.Normalize(Drift));
    }
}