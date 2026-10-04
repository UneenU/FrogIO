using System.Numerics;
using Core.Commands;
using Core.Events;

namespace Core;

public interface IEventSink
{
    void Publish(GameEvent e);
}


public class Match : IEventSink
{
    private readonly Queue<GameEvent> _events = new();
    private readonly Map _map;
    private readonly ICommandHandler _commandHandler;
    private readonly IReadOnlyList<ISimulationSystem> _systems;

    public Gamer[] Gamers { get; init; }
    public int Tick { get; private set; }

    /// <param name="systems">Порядок в списке = порядок выполнения внутри тика (движение, потом приземление).</param>
    public Match(Map map, Gamer[] gamers, ICommandHandler commandHandler, IReadOnlyList<ISimulationSystem> systems)
    {
        _map = map;
        Gamers = gamers;
        _commandHandler = commandHandler;
        _systems = systems;
    }

    // Явная реализация: снаружи Match события публиковать нельзя, только системы через контекст.
    void IEventSink.Publish(GameEvent e) => _events.Enqueue(e);

    public void Step(Command[] userCommands)
    {
        var ctx = new SimulationContext(_map, Tick, this);

        _commandHandler.Handle(userCommands, ctx); // намерения -> состояние сущностей (TryStartJump, Aim, Swim)

        foreach (var system in _systems) // MovementSystem, LandingSystem, ...
            system.Update(ctx);

        DispatchEvents();
        Tick++;
    }

    private void DispatchEvents()
    {
        while (_events.TryDequeue(out var e))
        {
            // TODO: найти IEventHandler<T> по типу события.
            // Новые события, опубликованные обработчиками, попадут в ту же очередь (поставить лимит итераций).
        }
    }
}