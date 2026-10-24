using Core.Events;
using Core.Match;

namespace Core.Systems.Abstraction;

public readonly record struct SimulationContext(
    MapSystem.Map Map,
    int Tick,
    IEventSink Events,
    Player[] Players,
    List<ClientEvent> ClientEvents,
    TimeOfDay TimeOfDay = TimeOfDay.Day);