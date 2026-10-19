using Core.Objects.Frogs;
using Core.Objects.Frogs.Tongues;
using Core.Objects.Storks;

namespace Core.Match;

public record MatchState(
    int Tick,
    float Time,
    float TimeLeft,
    bool IsEnded,
    FrogInfo[] Frogs,
    PadState[] Pads,
    FlyState[] Flies,
    SplashState[] Splashes,
    PlayerScore[] Scores,
    StorkState[] Storks);

public record PadState(int Id, float X, float Y, float Radius);

public record FlyState(int Id, string Type, float X, float Y, float Radius, float Angle);

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
    TongueState Tongue,
    TongueInfo TongueDetails);


public record SplashState(int Id, float X, float Y, float Radius);

public record PlayerScore(int PlayerId, int Score);

public record StorkState(int Id, float X, float Y, float Radius, float Height, float Angle, StorkPhase Phase);