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

public class TwoObjectsCollidedEvent(ICollidable obj1, ICollidable obj2) : GameEvent
{
    public ICollidable Obj1 { get; init; } = obj1;
    public ICollidable Obj2 { get; init; } = obj2;
}

public class FrogEatFlyEvent(Frog frog, Fly fly) : GameEvent
{
    public Frog Frog { get; init; } = frog;
    public Fly Fly { get; init; } = fly;
}