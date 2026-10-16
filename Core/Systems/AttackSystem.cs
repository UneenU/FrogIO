using System.Numerics;
using Core.Events;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public class AttackSystem : ISimulationSystem
{
    public void Update(SimulationContext ctx)
    {
        foreach (var frog in ctx.Map.Query<Frog>())
        {
            if (!frog.IsAttacking) continue;

            if (frog.Tongue.Update(frog, ctx))
                frog.EndAttack();
        }
    }
}
