using Core.Objects;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class RespawnSystem(Random? random = null) : ISimulationSystem
{
    private const int RespawnDelayInTicks = 5 * Constants.TicksInSecond;
    private readonly Dictionary<Frog, int> _ticksUntilRespawn = new();
    private readonly Random _random = random ?? new Random();

    public void Update(SimulationContext ctx)
    {
        var deadFrogs = ctx.Players
            .Select(p => p.Controllable)
            .OfType<Frog>()
            .Where(f => !f.IsAlive);
        foreach (var deadFrog in deadFrogs)
        {
            if (_ticksUntilRespawn.TryAdd(deadFrog, RespawnDelayInTicks)) continue;

            _ticksUntilRespawn[deadFrog]--;
        }

        foreach (var (frog, ticks) in _ticksUntilRespawn)
        {
            if (ticks != 0) continue;

            // Возрождаем только на кувшинках, которые ещё в пределах платформы: остальные уже тонут.
            var lilies = ctx.Map.Query<WaterLily>().Where(l => ctx.Map.Platform.Contains(l.Position)).ToList();
            if (lilies.Count == 0) continue;

            _ticksUntilRespawn.Remove(frog);
            var lily = lilies[_random.Next(lilies.Count)];
            frog.Respawn(lily);
            ctx.Map.Spawn(frog);
        }
    }
}
