using System.Numerics;

namespace Core.Objects.Abstraction;

public interface ICollidable
{
    Vector2 Position { get; }
    float Mass { get; }
    float Radius { get; }
    bool CollidesWith(ICollidable other) => true;
}