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

    public void AssignControllable(IControllable controllable)
    {
        Controllable = controllable;
    }
}