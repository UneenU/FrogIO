using System.Numerics;
using Core;
using Core.Events;

namespace Core;

/// <summary>Переводит живой мир (Map) в неизменяемый снимок для клиентов.</summary>
public static class MatchStateMapper
{
    public static MatchState Create(Map map, int tick, float time, float timeLeft, PlayerScore[] scores) =>
        new(tick,
            time,
            timeLeft,
            map.Query<Frog>().Select(ToFrogState).ToArray(),
            map.Query<WaterLily>().Select(ToPadState).ToArray(),
            map.Query<Fly>().Select(ToFlyState).ToArray(),
            scores);

    private static PadState ToPadState(WaterLily lily) =>
        new(lily.Id, lily.Position.X, lily.Position.Y, lily.Radius);

    private static FlyState ToFlyState(Fly fly) =>
        new(fly.Id, fly.Position.X, fly.Position.Y, fly.Radius, AngleOf(fly.Facing));

    private static FrogInfo ToFrogState(Frog frog)
    {
        var tip = frog.TonguePosition;
        return new FrogInfo(
            frog.Id,
            frog.Position.X,
            frog.Position.Y,
            frog.Radius,
            AngleOf(frog.Facing),
            frog.State,
            frog.Lily?.Id,
            frog.TonguePosition.X,
            frog.TonguePosition.Y,
            frog.TongueState);
    }

    private static float AngleOf(Vector2 direction) => MathF.Atan2(direction.Y, direction.X);
}