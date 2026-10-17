using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.Objects.Storks;

public enum StorkPhase
{
    Searching,
    Descending,
    Ascending,
    Leaving,
}

public sealed class Stork : GameObject
{
    private readonly StorkSettings _settings;
    private readonly int _descentTicks;
    private readonly int _ascentTicks;
    private int _phaseTicks;

    public Stork(Vector2 origin, Vector2 direction, float routeLength, StorkSettings settings) : base(origin, settings.Radius)
    {
        Position = origin;
        RouteLength = routeLength;
        _settings = settings;
        _descentTicks = Math.Max(1, settings.DescentTicks);
        _ascentTicks = Math.Max(1, settings.AscentTicks);

        Direction = Vector2.Normalize(direction);
        Height = settings.CruiseHeight;
        Speed = settings.Speed;
    }

    public int Id { get; set; }
    public Vector2 Direction { get; }
    public float Speed { get; private set; }
    public float RouteLength { get; }
    public float Distance { get; private set; }
    public StorkPhase Phase { get; private set; } = StorkPhase.Searching;

    public Vector2 LandingPoint { get; private set; }

    /// <summary>Точка, куда аист приземлится, если начнёт снижение прямо сейчас.</summary>
    public Vector2 LookAheadPoint => Position + Direction * _settings.LookAheadDistance;

    public bool RouteFinished => Distance >= RouteLength + Radius - Constants.Epsilon;

    /// <summary>
    /// Можно ли начинать снижение: аист ещё не охотился и успеет приземлиться до конца маршрута
    /// (иначе он улетел бы за карту, не долетев до земли).
    /// </summary>
    public bool CanStartDescent =>
        Phase == StorkPhase.Searching && Distance + _settings.LookAheadDistance <= RouteLength;

    public void BeginDescent()
    {
        if (!CanStartDescent) return;

        Speed = _settings.SpeedWileAttacking;
        LandingPoint = LookAheadPoint;
        Phase = StorkPhase.Descending;
        _phaseTicks = 0;
    }

    /// <summary>
    /// Раз в тик: летит вперёд и обновляет высоту.
    /// Возвращает true в тик касания земли
    /// </summary>
    public bool Advance()
    {
        Distance = MathF.Min(Distance + Speed, RouteLength);
        Position += Direction * Speed;

        switch (Phase)
        {
            case StorkPhase.Descending:
                _phaseTicks++;
                Height = _settings.CruiseHeight * (1f - (float)_phaseTicks / _descentTicks);
                if (_phaseTicks < _descentTicks) return false;
                Height = 0f;
                return true;

            case StorkPhase.Ascending:
                _phaseTicks++;
                Height = _settings.CruiseHeight * ((float)_phaseTicks / _ascentTicks);
                if (_phaseTicks < _ascentTicks) return false;
                Height = _settings.CruiseHeight;
                Phase = StorkPhase.Leaving;
                return false;

            default:
                return false;
        }
    }

    public void BeginAscent()
    {
        Phase = StorkPhase.Ascending;
        Speed = _settings.Speed;
        _phaseTicks = 0;
    }
}