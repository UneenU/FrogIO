using Core.Objects.Abstraction;
using Core.Systems;

namespace Core.Commands;


public interface IAiController
{
    int ControllerId { get; }

    Command Think(IControllable body, SimulationContext ctx);
}

public class AiControllerFactory
{
    private static int controllerId = 0;

    public IAiController NewFlyAiController()
    {
        controllerId++;
        return new FlyAiController(controllerId);
    }

    public IAiController NewFrogAiController()
    {
        controllerId++;
        return new FrogAiController(controllerId);
    }
}