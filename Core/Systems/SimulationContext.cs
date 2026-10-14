using Core.Events;

namespace Core.Systems;

public readonly record struct SimulationContext(MapSystem.Map Map, int Tick, IEventSink Events);