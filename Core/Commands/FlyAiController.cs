using System.Numerics;
using Core.Objects.Abstraction;
using Core.Systems;

namespace Core.Commands;

public class FlyAiController(int controllerId) : IAiController
{
    private const int RetargetEveryTicks = 100;
    private readonly Random _random = new();
    private int _retargetAtTick;

    public Vector2 Target { get; private set; } = Vector2.Zero;
    public int ControllerId { get; } = controllerId;

    private Vector2 ChooseTarget()
    {
        return new Vector2(_random.Next(100, 500), _random.Next(100, 300));
    }

    public Command Think(IControllable body, SimulationContext ctx)
    {
        var tick = ctx.Tick;
        if (tick >= _retargetAtTick)
        {
            Target = ChooseTarget();
            _retargetAtTick = tick + RetargetEveryTicks;
        }

        return new Command(ControllerId, Target, CommandButtons.Move);
    }
}