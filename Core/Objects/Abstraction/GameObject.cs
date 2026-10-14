using System.Numerics;

namespace Core.Objects.Abstraction;

public abstract class GameObject
{
    public int Id { get; set; }
    public Vector2 Position { get; protected set; }
    public float Height { get; protected set; }
    public float Radius { get; protected set; }
    
    protected GameObject(Vector2 position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}
