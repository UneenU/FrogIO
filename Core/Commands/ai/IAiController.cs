using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
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
    public IAiController<Fly> NewFlyAiController(Fly fly, Random? random = null)
    {
        if (fly is Mosquito mosquito)
            return new MosquitoAiController(mosquito, random);

        if (fly is Firefly firefly)
            return new FireflyAiController(firefly, random);

        return new FlyAiController(fly, random);
    }

    public IAiController<Frog> NewFrogAiController(Frog frog, Random? random = null)
    {
        return new FrogAiController(frog, random);
    }
}
