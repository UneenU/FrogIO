using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Commands;

public interface IAiController
{
    Command Think(SimulationContext ctx);
}

public interface IAiController<out T> : IAiController where T : IControllable
{
    T Controllable { get; }
}

public class AiControllerFactory
{
    public IAiController<Fly> NewFlyAiController(Fly fly)
    {
        return new FlyAiController(fly);
    }

    public IAiController<Frog> NewFrogAiController(Frog frog)
    {
        return new FrogAiController(frog);
    }
}