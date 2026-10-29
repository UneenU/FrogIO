using Core.Commands;
using Core.Events;
using Core.MapSystem;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Match;

public class Match
{
    private readonly Time _time = new Time(18, 0);
    private readonly Map _map;
    private readonly ScoreBoard _scoreBoard;
    private readonly ControlSystem _controlSystem;
    private readonly MapNarrowingSystem _mapNarrowingSystem;
    private readonly AiInputSystem _aiSystem;
    private readonly IReadOnlyList<ISimulationSystem> _systems;
    private readonly EventDispatcher _events;
    private readonly MatchSettings _settings;
    private MatchState _lastState;
    private readonly List<ClientEvent> _clientEvents = [];

    public Player[] Players { get; }
    public int Tick { get; private set; }
    public string Id { get; }
    public float WorldWidth => _map.Width;
    public float WorldHeight => _map.Height;
    public float MatchDurationSeconds => _settings.DurationSeconds;

    public TimeOfDay TimeOfDay = TimeOfDay.Day;
    private readonly LevelingSystem _levelingSystem;

    public bool IsEnded => _lastState?.IsEnded ?? false;


    public Match(
        string id,
        Map map,
        Player[] players,
        ControlSystem controlSystem,
        LevelingSystem levelingSystem,
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
        _mapNarrowingSystem = new MapNarrowingSystem(_map, 5, 17 * 60, 3 * 60);
        _settings = settings ?? MatchSettings.Default;
        _levelingSystem = levelingSystem;
        _scoreBoard = new ScoreBoard(players);
        _events.RegisterHandler(_scoreBoard);
    }

    public (MatchState, ClientEvent[] clientEvents) Step(Command[] usersCommands, ContextCommand[] usersContextCommands)
    {
        if (IsEnded) return (_lastState, []);

        _clientEvents.Clear();
        var ctx = new SimulationContext(_map, Tick, _events, Players, _clientEvents, TimeOfDay);
        var commands = _aiSystem.GetAiCommands(ctx);
        commands.AddRange(usersCommands);

        _mapNarrowingSystem.Update(ctx);
        _controlSystem.Handle(commands, ctx);
        _levelingSystem.Handle(usersContextCommands, ctx);

        foreach (var system in _systems)
            system.Update(ctx);

        _events.DispatchAll(ctx);
        _map.ApplyPendingChanges();
        Tick++;

        if (Tick % _settings.TickRate == 0)
        {
            _time.AddMinutes(_settings.WorldMinutesInOneSecond);
            TimeOfDay = _time >= _settings.DayStart && _time <= _settings.DayEnd ? TimeOfDay.Day : TimeOfDay.Night;
        }

        var state = BuildState();
        _lastState = state;
        return (state, _clientEvents.ToArray());
    }


    public MatchResult GetResult()
    {
        return new MatchResult { Scores = _scoreBoard.Snapshot() };
    }

    private MatchState BuildState()
    {
        var time = Tick * _settings.TickDuration;
        var timeLeft = MathF.Max(0f, _settings.DurationSeconds - time);
        var state = MatchStateMapper.Create(_map, Tick, time, timeLeft, _time.Hours, _scoreBoard.Snapshot(),
            _mapNarrowingSystem.CurrentMapState());
        return state;
    }
}

public record MatchResult
{
    public Dictionary<int, int> Scores { get; init; }
}

public record ContextCommand(int PlayerId, int ChoiceNumber);