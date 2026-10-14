using Core.Commands;
using Core.Events;
using Core.MapSystem;
using Core.Systems;
using Core.Systems.Interfaces;
namespace Core.Match;

public class Match
{
    
    // TODO: вернуть в private
    private readonly Map _map;
    private readonly ControlSystem _controlSystem;
    private readonly AiInputSystem _aiSystem;
    private readonly IReadOnlyList<ISimulationSystem> _systems;
    private readonly EventDispatcher _events;
    private readonly MatchSettings _settings;
    private readonly ScoreBoard _scoreBoard;
 
    public Player[] Players { get; }
    public int Tick { get; private set; }
    public string Id { get; }
    public float WorldWidth => _map.Width;
    public float WorlHeight => _map.Height;
    

    public Match(
        string id,
        Map map,
        Player[] players,
        ControlSystem controlSystem,
        AiInputSystem aiSystem,
        IReadOnlyList<ISimulationSystem> systems,
        EventDispatcher events, MatchSettings? settings = null)
    {
        Id = id;
        _map = map;
        Players = players;
        _controlSystem = controlSystem;
        _aiSystem = aiSystem;
        _systems = systems;
        _events = events;
        _settings = settings ?? MatchSettings.Default;
        
        _scoreBoard = new ScoreBoard(players);
        _events.RegisterHandler(_scoreBoard);
    }
    
    public MatchState Step(Command[] usersCommands)
    {
        var ctx = new SimulationContext(_map, Tick, _events);
        var commands = _aiSystem.GetAiCommands(ctx);
        commands.AddRange(usersCommands);
 
        _controlSystem.Handle(commands, ctx);
 
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