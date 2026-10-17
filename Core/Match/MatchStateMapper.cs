using System.Numerics;
using Core.Objects;
using Core.Objects.Frogs;
using Core.Objects.Storks;

namespace Core.Match;

public static class MatchStateMapper
{
    public static MatchState Create(MapSystem.Map map, int tick, float time, float timeLeft, PlayerScore[] scores) =>
        new(tick,
            time,
            timeLeft,
            map.Query<Frog>().Select(ToFrogState).ToArray(),
            map.Query<WaterLily>().Select(ToPadState).ToArray(),
            map.Query<Fly>().Select(ToFlyState).ToArray(),
            map.Query<WaterSplash>().Select(ToSplashState).ToArray(),
            scores,
            map.Query<Stork>().Select(ToStorkState).ToArray());

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
            frog.TongueState,
            frog.Tongue.ToInfo(frog));
    }

    private static SplashState ToSplashState(WaterSplash splash)
    {
        return new SplashState(splash.Id, splash.Position.X, splash.Position.Y, splash.Radius);
    }

    private static StorkState ToStorkState(Stork stork) =>
        new(stork.Id, stork.Position.X, stork.Position.Y, stork.Radius, stork.Height, AngleOf(stork.Direction), stork.Phase);


    private static float AngleOf(Vector2 direction) => MathF.Atan2(direction.Y, direction.X);
}