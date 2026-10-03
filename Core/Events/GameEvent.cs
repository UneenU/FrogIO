namespace Core.Events;

public class GameEvent
{
}

public class FrogJumpedInWaterEvent : GameEvent
{
    private Frog Frog { get; init; }

    public FrogJumpedInWaterEvent(Frog frog)
    {
        Frog = frog;
    }
}

public class FrogLandOnWaterLilyEvent : GameEvent
{
    private Frog Frog { get; init; }
    private WaterLily WaterLily { get; init; }

    public FrogLandOnWaterLilyEvent(Frog frog, WaterLily waterLily)
    {
        Frog = frog;
        WaterLily = waterLily;
    }
}