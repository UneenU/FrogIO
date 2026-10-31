using Core.MapSystem;
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
    PlatformState Platform,
    FrogInfo[] Frogs,
    PadState[] Pads,
    FlyState[] Flies,
    SplashState[] Splashes,
    StorkState[] Storks,
    Dictionary<int, int> Scores);

public record PadState(int Id, float X, float Y, float Height, float Radius);

public record FlyState(int Id, string Type, string Effect, float X, float Y, float Height, float Radius, float Angle);

public record FrogInfo(
    int Id,
    float X,
    float Y,
    float Height,
    float Radius,
    float Angle,
    FrogState State,
    string[] Effects,
    int? PadId,
    TongueInfo TongueDetails);


public record SplashState(int Id, float X, float Y, float Radius);

public record StorkState(int Id, float X, float Y, float Radius, float Height, float Angle, StorkPhase Phase);

public record RectState(float X, float Y, float Width, float Height);

/// <param name="ShrinkTicksLeft">Тиков до следующей смены фазы сужения.</param>
/// <param name="NextBounds">Границы после ближайшего сужения</param>
public record PlatformState(
    float X,
    float Y,
    float Width,
    float Height,
    ShrinkPhase ShrinkPhase,
    int ShrinkTicksLeft,
    RectState? NextBounds);