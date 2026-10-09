using Core.Commands;
using Core.Events;
using Server;

namespace Core;

public interface IEventSink
{
    void Publish(GameEvent e);
}


public class Match
{
    private readonly Map _map;
    private readonly ICommandHandler _commandHandler;
    private readonly IReadOnlyList<ISimulationSystem> _systems;
    private readonly EventDispatcher _events;
 
    public Gamer[] Gamers { get; init; }
    public int Tick { get; private set; }
    public string Id { get; }

    /// <param name="systems">Порядок в списке = порядок выполнения внутри тика (движение, потом приземление).</param>
    public Match(
        Map map,
        Gamer[] gamers,
        ICommandHandler commandHandler,
        IReadOnlyList<ISimulationSystem> systems,
        EventDispatcher events)
    {
        _map = map;
        Gamers = gamers;
        _commandHandler = commandHandler;
        _systems = systems;
        _events = events;
    }
    
    public MatchState Step(Command[] userCommands)
    {
        var ctx = new SimulationContext(_map, Tick, _events);
 
        _commandHandler.Handle(userCommands, ctx);
 
        foreach (var system in _systems)
            system.Update(ctx);
 
        _events.DispatchAll(ctx);
        _map.ApplyPendingChanges();
        Tick++;

        return new();
    }
}