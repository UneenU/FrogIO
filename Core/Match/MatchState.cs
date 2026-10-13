namespace Core;

/// <summary>Снимок мира после тика. Это DTO для клиентов: ссылок на объекты ядра в нём нет.</summary>
public record MatchState(
    int Tick,
    float Time,
    float TimeLeft,
    FrogInfo[] Frogs,
    PadState[] Pads,
    FlyState[] Flies,
    PlayerScore[] Scores);

public record PadState(int Id, float X, float Y, float Radius);

public record FlyState(int Id, float X, float Y, float Radius, float Angle);

/// <param name="Angle">Куда смотрит лягушка, радианы: Atan2(Facing.Y, Facing.X).</param>
/// <param name="State">Флаги состояния ядра (Core.FrogState), например Sitting | Attacking.</param>
/// <param name="PadId">Id кувшинки, на которой сидит лягушка; null, если в прыжке или плывёт.</param>
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

public record PlayerScore(int PlayerId, int Score);
