using System.Numerics;

namespace Core;

public interface IMoveSystem
{
    public void Move(IList<IMovable> objectsToMove, int tick);
}
