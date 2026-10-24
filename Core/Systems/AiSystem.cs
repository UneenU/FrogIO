using Core.Commands;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class AiInputSystem(List<IAiController> aiControllers)
{
    private readonly HashSet<GameObject> _alive = [];

    public void Add(IAiController controller) => aiControllers.Add(controller);

    public List<Command> GetAiCommands(SimulationContext ctx)
    {
        RemoveControllersOfGoneFlies(ctx);

        List<Command> commands = new();
        foreach (var aiController in aiControllers)
        {
            commands.Add(aiController.Think(ctx));
        }

        return commands;
    }
    
    private void RemoveControllersOfGoneFlies(SimulationContext ctx)
    {
        _alive.Clear();
        foreach (var obj in ctx.Map.GameObjects)
            _alive.Add(obj);

        aiControllers.RemoveAll(c => c is IAiController<Fly> { Controllable: var fly } && !_alive.Contains(fly));
    }
}