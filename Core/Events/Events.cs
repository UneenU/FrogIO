using System.Numerics;
using Core.MapSystem;
using Core.Match;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Objects.Storks;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Events;

public abstract record GameEvent{}

public abstract record ClientEvent {}

public record FrogFallInWaterEvent(Frog Frog) : GameEvent;

public record FrogLandOnWaterLilyEvent(Frog Frog, WaterLily WaterLily) : GameEvent;

public record TwoObjectsCollidedEvent(ICollidable Obj1, ICollidable Obj2) : GameEvent;

public record FrogEatFlyEvent(Frog Frog, Fly Fly) : GameEvent;

public record StorkCaughtFrogEvent(Stork Stork, Frog Frog) : GameEvent;

public record PlayerGetNewLevelEvent(int PlayerId, LevelListPosition[] Positions) : ClientEvent;

public record ObjectLeftPlatform(GameObject Obj, Platform Platform) : GameEvent;
