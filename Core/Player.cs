using Core.Objects.Abstraction;

namespace Core;

public class Player
{
    public int PlayerId { get; init; }
    public IControllable Controllable { get; private set; }
    
    
    public Player(int playerId, IControllable controllable)
    {
        PlayerId = playerId;
        Controllable = controllable;
    }
}