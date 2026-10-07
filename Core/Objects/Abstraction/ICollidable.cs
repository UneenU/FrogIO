using System.Numerics;

namespace Core;

public interface ICollidable
{
    Vector2 Position { get; }
    float Radius { get; }
    bool CollidesWith(ICollidable other) => true;
}