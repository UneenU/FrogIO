using Core.Objects;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class RespawnSystem : ISimulationSystem
{
    private const int RespawnDelayInTicks = 5 * Constants.TicksInSecond;
    private readonly Dictionary<Frog, int> _ticksUntilRespawn = new();
    private readonly Random _random = new();

    public void Update(SimulationContext ctx)
    {
        var deadFrogs = ctx.Players
            .Select(p => p.Controllable)
            .OfType<Frog>()
            .Where(f => !f.IsAlive);
        foreach (var deadFrog in deadFrogs)
        {
            if (_ticksUntilRespawn.TryAdd(deadFrog, RespawnDelayInTicks)) continue;

            _ticksUntilRespawn[deadFrog] -= 1;
        }

        foreach (var (frog, ticks) in _ticksUntilRespawn)
        {
            if (ticks != 0) continue;

            _ticksUntilRespawn.Remove(frog);
            var lilies = ctx.Map.Query<WaterLily>();
            var lily = lilies[_random.Next(lilies.Count)];
            frog.Respawn(lily);
            ctx.Map.Spawn(frog);
        }
    }
}