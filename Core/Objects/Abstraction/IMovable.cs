using System.Numerics;

namespace Core;

public interface IMovable
{
    Vector2 Velocity { get; }
    Vector2 Drift { get; }
    sealed Vector2 TotalVelocity => Velocity + Drift;
    void MoveBy(Vector2 delta);
    void AddImpulse(Vector2 deltaV);

    void DampDrift(float delta)
    {
        AddImpulse(-delta*Vector2.Normalize(Drift));
    }

}