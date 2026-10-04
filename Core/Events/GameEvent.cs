namespace Core.Events;

public class GameEvent
{
}

public class FrogFallInWaterEvent(Frog frog) : GameEvent
{
    public Frog Frog { get; init; } = frog;
}

public class FrogLandOnWaterLilyEvent(Frog frog, WaterLily waterLily) : GameEvent
{
    public Frog Frog { get; init; } = frog;
    public WaterLily WaterLily { get; init; } = waterLily;
}

public class TwoObjectsCollided: GameEvent
{
    public GameObject obj1 { get; init; }
    public GameObject obj2 { get; init; }
}