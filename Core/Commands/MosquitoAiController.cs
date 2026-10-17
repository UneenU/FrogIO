using System.Numerics;
using Core.Objects;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Commands;

public sealed class MosquitoAiController(Mosquito controllable) : IAiController<Mosquito>
{
    private const float FrogInterestRadius = 100f;
    private const float PanicDistance = 40f;
    private const float AttackReactionDistance = 60f;

    private readonly Random _random = new();
    private Vector2 _target;
    private int _nextDecisionTick;
    private int _fleeUntilTick;
    private float _orbitAngle;
    private int _orbitDirection = Random.Shared.Next(2) == 0 ? -1 : 1;
    private bool _hasTarget;
    private Frog? _orbitingFrog;

    public Mosquito Controllable { get; } = controllable;

    public Command Think(SimulationContext ctx)
    {
        var mosquito = Controllable;
        var tick = ctx.Tick;

        if (mosquito.CaughtBy is not null)
            return MoveTo(mosquito.Position);

        var frog = FlyAiHelpers.FindNearestFrog(mosquito, ctx.Map, FrogInterestRadius);
        if (frog is not null)
        {
            var distance = Vector2.Distance(mosquito.Position, frog.Position);
            var mustDodge = distance < PanicDistance ||
                            (frog.IsAttacking && distance < AttackReactionDistance);

            if (mustDodge)
            {
                _fleeUntilTick = Math.Max(_fleeUntilTick, tick + _random.Next(10, 17));

                if (!_hasTarget || tick >= _nextDecisionTick || HasReachedTarget(mosquito))
                {
                    _target = FlyAiHelpers.EscapeTarget(
                        mosquito, frog, ctx.Map, _random, minDistance: 80f, maxDistance: 135f);
                    _nextDecisionTick = tick + _random.Next(3, 8);
                    _hasTarget = true;
                }

                return MoveTo(_target);
            }

            if (tick < _fleeUntilTick && _hasTarget)
                return MoveTo(_target);

            if (!ReferenceEquals(_orbitingFrog, frog))
            {
                _orbitingFrog = frog;
                var offset = mosquito.Position - frog.Position;
                _orbitAngle = offset.LengthSquared() > 1e-4f
                    ? MathF.Atan2(offset.Y, offset.X)
                    : _random.NextSingle() * MathF.Tau;
                _nextDecisionTick = tick;
            }

            if (!_hasTarget || tick >= _nextDecisionTick || HasReachedTarget(mosquito))
            {
                if (_random.NextSingle() < 0.16f)
                    _orbitDirection *= -1;

                _orbitAngle += _orbitDirection * (0.45f + _random.NextSingle() * 0.75f);
                var orbitRadius = 72f + _random.NextSingle() * 28f;
                var offset = new Vector2(MathF.Cos(_orbitAngle), MathF.Sin(_orbitAngle)) * orbitRadius;
                _target = FlyAiHelpers.ClampInsideMap(frog.Position + offset, mosquito, ctx.Map);
                _nextDecisionTick = tick + _random.Next(7, 15);
                _hasTarget = true;
            }

            return MoveTo(_target);
        }

        if (tick < _fleeUntilTick && _hasTarget)
            return MoveTo(_target);

        _orbitingFrog = null;

        if (!_hasTarget || tick >= _nextDecisionTick || HasReachedTarget(mosquito))
        {
            var angle = _random.NextSingle() * MathF.Tau;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            direction = FlyAiHelpers.BiasAwayFromEdges(mosquito.Position, direction, ctx.Map);
            var distance = 40f + _random.NextSingle() * 70f;
            _target = FlyAiHelpers.ClampInsideMap(mosquito.Position + direction * distance, mosquito, ctx.Map);
            _nextDecisionTick = tick + _random.Next(8, 19);
            _hasTarget = true;
        }

        return MoveTo(_target);
    }

    private bool HasReachedTarget(Mosquito mosquito)
    {
        var arrivalDistance = MathF.Max(mosquito.Radius * 2f, 8f);
        return _hasTarget && Vector2.DistanceSquared(mosquito.Position, _target) <= arrivalDistance * arrivalDistance;
    }

    private Command MoveTo(Vector2 target) => new()
    {
        Controllable = Controllable,
        Target = target,
        Buttons = CommandButtons.Move
    };
}
