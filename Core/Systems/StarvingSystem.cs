using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class StarvingSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        var frogs = ctx.Map.Query<Frog>();
        foreach (var frog in frogs)
        {
            
        }
    }
}