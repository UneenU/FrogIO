namespace Core;

public class Gamer
{
    public int GamerId { get; init; }
    public string Name { get; init; }
    public IControllable Controllable { get; init; }
    
    public Gamer(int gamerId, string name, IControllable controllable)
    {
        GamerId = gamerId;
        Name = name;
        Controllable = controllable;
    }
    
}