using System.Numerics;

namespace Core;

public interface IMovable
{
    public Vector2 Velocity { get; }
    public void MoveBy(Vector2 delta);

}