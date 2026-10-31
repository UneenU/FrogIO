using System.Numerics;

namespace Core.MapSystem;

public interface IFlowField
{
    Vector2 GetDisplacement(Vector2 position, Platform platform);
}
