using System.Numerics;

namespace Core.Objects.Abstraction;

public abstract class GameObject(Vector2 position, float radius)
{
    public int Id { get; set; } = -1;
    public Vector2 Position { get; protected set; } = position;
    public float Height { get; protected set; }
    public float Radius { get; protected internal set; } = radius;
}
