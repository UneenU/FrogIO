using Core.Commands;

namespace Core.Systems;

public class AiInputSystem(List<IAiController> aiControllers)
{
    public List<Command> GetAiCommands(SimulationContext ctx)
    {
        List<Command> commands = new();
        foreach (var aiController in aiControllers)
        {
            commands.Add(aiController.Think(ctx));
        }
        
        return commands;
    }
}