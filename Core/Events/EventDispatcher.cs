using Core.Match;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Events;

public interface IEventSink
{
    void Publish(GameEvent e);
}

public sealed class EventDispatcher : IEventSink
{
    private const int MaxEventsPerTick = 1000;
    private readonly Queue<GameEvent> _queue = new();
    private readonly Dictionary<Type, List<Action<GameEvent, SimulationContext>>> _handlers = new();


    public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler) where TEvent : GameEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            list = new List<Action<GameEvent, SimulationContext>>();
            _handlers[typeof(TEvent)] = list;
        }

        list.Add((e, ctx) => handler.Handle((TEvent)e, ctx));
    }

    public void Publish(GameEvent e) => _queue.Enqueue(e);


    public void DispatchAll(SimulationContext ctx)
    {
        var processed = 0;

        while (_queue.TryDequeue(out var e))
        {
            if (++processed > MaxEventsPerTick)
                throw new InvalidOperationException(
                    $"Больше {MaxEventsPerTick} событий за тик: последнее: {e.GetType().Name}.");

            if (!_handlers.TryGetValue(e.GetType(), out var handlers))
                continue;

            foreach (var handler in handlers)
                handler(e, ctx);
        }
    }
}
