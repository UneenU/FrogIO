using Core.Objects.Frogs;
using Core.Objects.Frogs.Tongues;
using Core.Objects.Storks;

namespace Core.Match;

public record MatchState(
    int Tick,
    float Time,
    float TimeLeft,
    bool IsEnded,
    int WorldHour,
    MapState MapState,
    FrogInfo[] Frogs,
    PadState[] Pads,
    FlyState[] Flies,
    SplashState[] Splashes,
    StorkState[] Storks,
    PlayerScore[] Scores);

public record PadState(int Id, float X, float Y, float Radius);

public record FlyState(int Id, string Type, string Effect, float X, float Y, float Radius, float Angle);

public record FrogInfo(
    int Id,
    float X,
    float Y,
    float Radius,
    float Angle,
    FrogState State,
    string[] Effects,
    int? PadId,
    TongueInfo TongueDetails);


public record SplashState(int Id, float X, float Y, float Radius);

public record PlayerScore(int PlayerId, int Score);

public record StorkState(int Id, float X, float Y, float Radius, float Height, float Angle, StorkPhase Phase);

public record MapState(float Width, float Height, bool IsNarrowing, float NarrowDistance);