using System.Numerics;
using Core.Objects;
using Core.Systems.Abstraction;

namespace Core.Commands;

public sealed class FireflyAiController(Firefly controllable, Random? random = null) : IAiController<Firefly>
{
    private const float HoverChance = 0.3f;

    private readonly Random _random = random ?? new Random();
    private Vector2 _target;
    private int _nextDecisionTick;
    private bool _hasTarget;
    private bool _hovering;

    public Firefly Controllable { get; } = controllable;

    public Command Think(SimulationContext ctx)
    {
        var firefly = Controllable;

        if (firefly.CaughtBy is not null)
            return Hold(firefly.Position);

        if (!_hasTarget || ctx.Tick >= _nextDecisionTick || (!_hovering && HasReachedTarget(firefly)))
            Decide(firefly, ctx);

        return _hovering ? Hold(firefly.Position) : MoveTo(_target);
    }

    private void Decide(Firefly firefly, SimulationContext ctx)
    {
        _hasTarget = true;
        _hovering = _random.NextSingle() < HoverChance;

        if (_hovering)
        {
            _nextDecisionTick = ctx.Tick + _random.Next(30, 91);
            return;
        }

        var angle = _random.NextSingle() * MathF.Tau;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        direction = FlyAiHelpers.BiasAwayFromEdges(firefly.Position, direction, ctx.Map);

        var distance = 20f + _random.NextSingle() * 40f;
        _target = FlyAiHelpers.ClampInsideMap(firefly.Position + direction * distance, firefly, ctx.Map);
        _nextDecisionTick = ctx.Tick + _random.Next(60, 151);
    }

    private bool HasReachedTarget(Firefly firefly)
    {
        var arrivalDistance = MathF.Max(firefly.Radius * 2f, 6f);
        return Vector2.DistanceSquared(firefly.Position, _target) <= arrivalDistance * arrivalDistance;
    }

    private Command MoveTo(Vector2 target) => new()
    {
        Controllable = Controllable,
        Target = target,
        Buttons = CommandButtons.Move
    };

    private Command Hold(Vector2 position) => new()
    {
        Controllable = Controllable,
        Target = position,
        Buttons = CommandButtons.None
    };
}
