using Core.Objects;
using Core.Objects.Abstraction;

namespace Core.Events;

public abstract record GameEvent{}

public record FrogFallInWaterEvent(Frog Frog) : GameEvent;

public record FrogLandOnWaterLilyEvent(Frog Frog, WaterLily WaterLily) : GameEvent;

public record TwoObjectsCollidedEvent(ICollidable Obj1, ICollidable Obj2) : GameEvent;

public record FrogEatFlyEvent(Frog Frog, Fly Fly) : GameEvent;