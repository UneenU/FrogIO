using Core.Events;

namespace Core;

public readonly record struct SimulationContext(Map Map, int Tick, IEventSink Events);