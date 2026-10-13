using Core.Events;

namespace Core.Systems;

public readonly record struct SimulationContext(Map.Map Map, int Tick, IEventSink Events);