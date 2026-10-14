using Core.Systems.Abstraction;

namespace Core.Systems;

public class StorkSpawnerSystem : ISimulationSystem
{
    private int _lastSpawnTick = 0;
    private readonly int _cooldownTicks = 500;
    private readonly Random _random = new();
    private readonly float _spawnPossibility = 0.1f;
    
    public void Update(SimulationContext ctx)
    {
        if (ctx.Tick >= _lastSpawnTick + _cooldownTicks || _random.NextSingle() > _spawnPossibility) return;
        
        _lastSpawnTick = ctx.Tick;
    }
}