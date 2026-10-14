using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Commands;

public class FlyAiController(Fly controllable) : IAiController<Fly>
{
    private readonly Random _random = new();
    
    private int _retargetAtTick;
    private const int RetargetEveryTicks = 100;
    private Vector2 _target = Vector2.Zero;

    public Fly Controllable { get; } = controllable;

    public Command Think(SimulationContext ctx)
    {
        var tick = ctx.Tick;
        if (tick >= _retargetAtTick)
        {
            _target = new Vector2(_random.NextSingle()*ctx.Map.Width, _random.NextSingle()*ctx.Map.Height);
            _retargetAtTick = tick + RetargetEveryTicks;
        }
        return new Command {Controllable = Controllable, Target = _target, Buttons = CommandButtons.Move};
    }
}