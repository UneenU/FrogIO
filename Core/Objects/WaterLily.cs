using System.Numerics;

namespace Core;

public class WaterLily : GameObject, IMovable
{
    public WaterLily(int id, Vector2 position, float radius) : base(id, position, radius)
    {
    }


    public Vector2 Velocity { get; set; }
    public void MoveBy(Vector2 delta)
    {
        Position += delta;
    }
}