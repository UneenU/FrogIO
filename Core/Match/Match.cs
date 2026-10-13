using Core.Commands;
using Core.Events;
using Core.Systems;
using Core.Systems.Interfaces;

namespace Core.Match;

public class Match
{
    
    // TODO: вернуть в private
    public readonly Map.Map _map;
    private readonly ICommandHandler _commandHandler;
    private readonly AiInputSystem _aiSystem;
    private readonly IReadOnlyList<ISimulationSystem> _systems;
    private readonly EventDispatcher _events;
    private readonly MatchSettings _settings;
    private readonly ScoreBoard _scoreBoard;
 
    public Player[] Players { get; init; }
    public int Tick { get; private set; }
    public string Id { get; init; }
    public float WorldWidth => _map.Width;
    public float WorlHeight => _map.Height;
    

    /// <param name="systems">Порядок в списке = порядок выполнения внутри тика (движение, потом приземление).</param>
    public Match(
        string id,
        Map.Map map,
        Player[] players,
        ICommandHandler commandHandler,
        AiInputSystem aiSystem,
        IReadOnlyList<ISimulationSystem> systems,
        EventDispatcher events, MatchSettings? settings = null)
    {
        Id = id;
        _map = map;
        Players = players;
        _commandHandler = commandHandler;
        _aiSystem = aiSystem;
        _systems = systems;
        _events = events;
        _settings = settings ?? MatchSettings.Default;
        
        _scoreBoard = new ScoreBoard(players);
        _events.RegisterHandler(_scoreBoard);
    }
    
    public MatchState Step(Command[] userCommands)
    {
        var ctx = new SimulationContext(_map, Tick, _events);
        var commands = _aiSystem.GetAiCommands(ctx);
        commands.AddRange(userCommands);
 
        _commandHandler.Handle(commands, ctx);
 
        foreach (var system in _systems)
            system.Update(ctx);
 
        _events.DispatchAll(ctx);
        _map.ApplyPendingChanges();
        Tick++;

        return BuildState();
    }
    
    private MatchState BuildState()
    {
        var time = Tick * _settings.TickDuration;
        var timeLeft = MathF.Max(0f, _settings.DurationSeconds - time);
        return MatchStateMapper.Create(_map, Tick, time, timeLeft, _scoreBoard.Snapshot());
    }
}