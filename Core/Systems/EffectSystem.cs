using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class EffectSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        var frogs = ctx.Map.Query<Frog>();
        foreach (var frog in frogs)
        {
            var effects = frog.ActiveTimedEffects;

            for (var i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                effect.Tick();
                if (effect.IsExpired)
                    frog.RemoveEffect(effect);
            }
        }
    }
}