using Core.Objects.Abstraction;

namespace Core;

public class Player(int playerId, IControllable controllable)
{
    public int PlayerId { get; } = playerId;
    public IControllable Controllable { get; private set; } = controllable;
}