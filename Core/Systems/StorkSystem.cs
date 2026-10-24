using System.Numerics;
using Core.Events;
using Core.MapSystem;
using Core.Match;
using Core.Objects;
using Core.Objects.Frogs;
using Core.Objects.Storks;
using Core.Systems.Abstraction;

namespace Core.Systems;

/// <summary>
/// Спавн, полёт и охота аистов
/// <para>
/// Пока аист ищет, он каждый тик смотрит на точку впереди по курсу (<see cref="Stork.LookAheadPoint"/>).
/// Если там есть живая лягушка, он снижается по диагонали и приземляется ровно в эту точку.
/// В момент касания земли проверяется круг захвата вокруг точки приземления: если лягушка осталась
/// внутри него, публикуется <see cref="StorkCaughtFrogEvent"/>. После этого (поймал или нет) аист
/// взлетает и до конца маршрута больше не охотится.
/// </para>
/// </summary>
public sealed class StorkSystem(StorkSettings? settings = null, Random? random = null) : ISimulationSystem
{
    private readonly StorkSettings _settings = settings ?? StorkSettings.Default;
    private readonly Random _random = random ?? new Random();
    private int _nextSpawnAllowedTick;

    public void Update(SimulationContext ctx)
    {
        AdvanceStorks(ctx);
        if (ctx.TimeOfDay is TimeOfDay.Day)
            Spawn(ctx);
    }

    public void Spawn(SimulationContext ctx)
    {
        if (ctx.Tick < _nextSpawnAllowedTick) return;
        if (_random.NextSingle() >= _settings.SpawnChancePerTick) return;

        ctx.Map.Spawn(CreateStork(ctx.Map));
        _nextSpawnAllowedTick = ctx.Tick + _settings.MinSpawnIntervalTicks;
    }

    private Stork CreateStork(Map map)
    {
        var angle = _random.NextSingle() * MathF.Tau;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var through = new Vector2(_random.NextSingle() * map.Width, _random.NextSingle() * map.Height);

        // Прямая through + t * direction пересекает прямоугольник карты при t от tEnter до tExit.
        var (tEnter, tExit) = ClipLine(through, direction, map.Width, map.Height);

        return new Stork(through + direction * tEnter, direction, tExit - tEnter, _settings);
    }

    /// <summary>Метод плит (slab) для прямой и прямоугольника [0, width] x [0, height]; точка origin внутри него.</summary>
    private static (float Enter, float Exit) ClipLine(Vector2 origin, Vector2 direction, float width, float height)
    {
        var enter = float.NegativeInfinity;
        var exit = float.PositiveInfinity;

        Clip(origin.X, direction.X, width);
        Clip(origin.Y, direction.Y, height);
        return (enter, exit);

        void Clip(float o, float d, float size)
        {
            // Параллельна оси: по этой координате прямая не выходит за карту (origin внутри).
            if (MathF.Abs(d) < Constants.Epsilon) return;

            var t1 = -o / d;
            var t2 = (size - o) / d;
            enter = MathF.Max(enter, MathF.Min(t1, t2));
            exit = MathF.Min(exit, MathF.Max(t1, t2));
        }
    }

    public void AdvanceStorks(SimulationContext ctx)
    {
        var storks = ctx.Map.Query<Stork>().ToList();

        // С конца, чтобы можно было удалять аиста прямо во время обхода.
        for (var i = storks.Count - 1; i >= 0; i--)
        {
            var stork = storks[i];

            if (stork.Advance())
                Strike(stork, ctx);
            else if (stork.CanStartDescent && FindPrey(ctx.Map, stork.LookAheadPoint) is not null)
                stork.BeginDescent();

            if (stork.RouteFinished)
                ctx.Map.Despawn(stork);
        }
    }

    private void Strike(Stork stork, SimulationContext ctx)
    {
        var prey = FindPrey(ctx.Map, stork.LandingPoint);
        if (prey is not null)
            ctx.Events.Publish(new StorkCaughtFrogEvent(stork, prey));

        stork.BeginAscent();
    }

    /// <summary>Ближайшая к точке живая лягушка, чьё тело пересекает круг захвата.</summary>
    private Frog? FindPrey(Map map, Vector2 point) =>
        map.ObjectsInCircle<Frog>(point, _settings.CaptureRadius)
            .Where(frog => frog.IsAlive)
            .OrderBy(frog => Vector2.DistanceSquared(frog.Position, point))
            .FirstOrDefault();
}