using Core.Objects;

namespace Core.Match;

public record MatchState(
    int Tick,
    float Time,
    float TimeLeft,
    FrogInfo[] Frogs,
    PadState[] Pads,
    FlyState[] Flies,
    SplashState[] Splashes,
    PlayerScore[] Scores);

public record PadState(int Id, float X, float Y, float Radius);

public record FlyState(int Id, float X, float Y, float Radius, float Angle);

public record FrogInfo(
    int Id,
    float X,
    float Y,
    float Radius,
    float Angle,
    FrogState State,
    int? PadId,
    float TongueX,
    float TunguqY,
    TongueState Tongue);


public record SplashState(int Id, float X, float Y, float Radius);

public record PlayerScore(int PlayerId, int Score);