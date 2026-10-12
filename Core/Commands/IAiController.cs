using System.Numerics;
using Core;

namespace Core.Commands;

/// <summary>
/// Производитель команд для NPC. Видит тот же мир, что и игрок, и отвечает той же командой.
/// ControllerId и Seq в возвращённой команде система перезапишет сама.
/// </summary>
public interface IAiController
{
    int ControllerId { get; }

    Command Think(IControllable body, SimulationContext ctx);
}

public class FlyAiController : IAiController
{
    private const int RetargetEveryTicks = 100;

    private readonly Random _random = new();
    private int _seq;
    private int _retargetAtTick = 0;

    public Vector2 Target { get; private set; } = Vector2.Zero;
    public int ControllerId { get; }

    public FlyAiController(int controllerId)
    {
        ControllerId = controllerId;
    }

    public Command NextCommand(int tick)
    {
        if (tick >= _retargetAtTick)
        {
            Target = ChooseTarget();
            _retargetAtTick = tick + RetargetEveryTicks;
        }

        return new Command(ControllerId, ++_seq, Target, CommandButtons.Move);
    }

    private Vector2 ChooseTarget()
    {
        return new Vector2(_random.Next(100, 500), _random.Next(100, 300));
    }

    public Command Think(IControllable body, SimulationContext ctx)
    {
        return NextCommand(ctx.Tick);
    }
}

public class FrogAiController : IAiController
{
    public int ControllerId { get; set; }

    public FrogAiController(int controllerId)
    {
        ControllerId = controllerId;
    }

    public Command Think(IControllable body, SimulationContext ctx)
    {
        return NextCommand(ctx.Tick, ctx.Map, (Frog)body);
    }

    private const int RetargetEveryTicks = 300;
    private const float AimDepth = 0.75f;
    private readonly Random _random = new();
    private int _seq;
    private int _retargetAtTick = 0;
    public Vector2? Target { get; private set; }

    public Command NextCommand(int tick, Map map, Frog frog)
    {
        if (tick >= _retargetAtTick)
        {
            Target = ChooseTarget(map, frog);
            _retargetAtTick = tick + RetargetEveryTicks;
        }

        return new Command(ControllerId, ++_seq, Target ?? frog.Position, CommandButtons.Move);
    }

    private Vector2 ChooseTarget(Map map, Frog frog)
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

public class AiControllerFactory
{
    private static int controllerId = 0;

    public IAiController NewFlyAiController()
    {
        controllerId++;
        return new FlyAiController(controllerId);
    }

    public IAiController NewFrogAiController()
    {
        controllerId++;
        return new FrogAiController(controllerId);
    }
}