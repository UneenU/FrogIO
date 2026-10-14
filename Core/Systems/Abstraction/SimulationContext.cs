using Core.Events;

namespace Core.Systems.Abstraction;

public readonly record struct SimulationContext(MapSystem.Map Map, int Tick, IEventSink Events);