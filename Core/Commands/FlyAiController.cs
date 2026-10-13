using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems;

namespace Core.Commands;

public class FlyAiController(Fly controllable) : IAiController<Fly>
{
    private readonly Random _random = new();
    
    private int _retargetAtTick;
    private const int RetargetEveryTicks = 100;
    private Vector2 _target = Vector2.Zero;

    public Fly Controllable { get; } = controllable;

    private Vector2 ChooseTarget()
    {
        return new Vector2(_random.Next(100, 500), _random.Next(100, 300));
    }


    public Command Think(SimulationContext ctx)
    {
        var tick = ctx.Tick;
        if (tick >= _retargetAtTick)
        {
            _target = ChooseTarget();
            _retargetAtTick = tick + RetargetEveryTicks;
        }
        return new Command(Controllable, _target, CommandButtons.Move);
    }
}