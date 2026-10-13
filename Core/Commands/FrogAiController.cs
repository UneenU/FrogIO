using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems;

namespace Core.Commands;

public class FrogAiController(int controllerId) : IAiController
{
    private const int RetargetEveryTicks = 300;
    private const float AimDepth = 0.75f;
    private readonly Random _random = new();
    private int _retargetAtTick = 0;
    
    public Vector2? Target { get; private set; }
    public int ControllerId { get; set; } = controllerId;

    public Command Think(IControllable body, SimulationContext ctx)
    {
        var tick = ctx.Tick;
        Frog frog = (Frog)body;
        if (tick >= _retargetAtTick)
        {
            Target = ChooseTarget(ctx.Map, frog);
            _retargetAtTick = tick + RetargetEveryTicks;
        }

        return new Command(ControllerId, Target ?? frog.Position, CommandButtons.Move);
    }


    private Vector2 ChooseTarget(Map.Map map, Frog frog)
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