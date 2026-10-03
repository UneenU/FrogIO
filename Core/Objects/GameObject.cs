using System.Numerics;

namespace Core;

public abstract class GameObject
{
    public int Id { get; }
    public Vector2 Position { get; protected set; }
    public float Height { get; protected set; }
    public float Radius { get; }
    
    protected GameObject(int id, Vector2 position, float radius)
    {
        Id = id;
        Position = position;
        Radius = radius;
    }
}
