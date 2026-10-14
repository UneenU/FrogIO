using System.Numerics;

namespace Core.Objects.Abstraction;

public abstract class GameObject(Vector2 position, float radius)
{
    public int Id { get; set; }
    public Vector2 Position { get; protected set; } = position;
    public float Height { get; protected set; }
    public float Radius { get; protected set; } = radius;
}
