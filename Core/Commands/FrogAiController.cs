using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Commands;

public class FrogAiController(Frog controllable) : IAiController<Frog>
{
    private readonly Random _random = new();

    private const float AimDepth = 0.75f;
    private const int RetargetEveryTicks = 300;
    private int _retargetAtTick = 0;
    private Vector2? _target;

    public Frog Controllable { get; } = controllable;

    public Command Think(SimulationContext ctx)
    {
        var tick = ctx.Tick;
        Frog frog = (Frog)Controllable;
        if (tick >= _retargetAtTick)
        {
            _target = ChooseTarget(ctx.Map, frog);
            _retargetAtTick = tick + RetargetEveryTicks;
        }

        return new Command
            { Controllable = Controllable, Target = _target ?? frog.Position, Buttons = CommandButtons.Move };
    }

    private Vector2 ChooseTarget(MapSystem.Map map, Frog frog)
    {
        var lilies = map.Query<WaterLily>()
            .Where(l => l != frog.Lily)
            .ToList();

        if (lilies.Count == 0)
            return RandomPointInReach(frog);

        return AimPoint(RandomLily(lilies), frog);
    }

    private WaterLily RandomLily(List<WaterLily> lilies) => lilies[_random.Next(lilies.Count)];

    private Vector2 AimPoint(WaterLily lily, Frog frog)
    {
        var toLily = lily.Position - frog.Position;
        if (toLily.LengthSquared() < 1e-6f)
            return lily.Position;

        return lily.Position - Vector2.Normalize(toLily) * (lily.Radius - AimDepth * lily.Radius);
    }

    private Vector2 RandomPointInReach(Frog frog)
    {
        var angle = _random.NextSingle() * MathF.Tau;
        var distance = 1f + _random.NextSingle() * (frog.MaxJumpDistance - 1f);
        return frog.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }
}