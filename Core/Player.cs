namespace Core;

public class Player
{
    public int PlayerId { get; init; }
    
    public int ControllerId { get; init; }
    
    public IControllable Controllable { get; private set; }
    
    
    public Player(int playerId, IControllable controllable, int controllerId)
    {
        PlayerId = playerId;
        Controllable = controllable;
        ControllerId = controllerId;
    }

    public void AssignControllable(IControllable controllable)
    {
        Controllable = controllable;
    }
}