using Core.Commands;

namespace Core.Systems;

public class AiInputSystem(List<IAiController> aiControllers, ControllerBindings bindings)
{
    private readonly List<IAiController> aiControllers = aiControllers;
    private readonly ControllerBindings bindings = bindings;

    public List<Command> GetAiCommands(SimulationContext ctx)
    {
        List<Command> commands = new();
        foreach (var aiController in aiControllers)
        {
            if(!bindings.TryGetBody(aiController.ControllerId, out var body)) continue;
            commands.Add(aiController.Think(body, ctx));
        }
        
        return commands;
    }
}