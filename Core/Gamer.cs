namespace Core;

public class Gamer
{
    public int GamerId { get; init; }
    public string Name { get; init; }
    public Frog Frog { get; init; }
    
    public Gamer(int gamerId, string name, Frog frog)
    {
        GamerId = gamerId;
        Name = name;
        Frog = frog;
    }
    
}