using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;
using Core;
using Core.Commands;
using Core.Events;
using Core.Match;
using Server.Match.MatchService;

namespace Server.Match;

public class MatchRuntime
{
    private Task? _loopTask;
    private readonly CancellationTokenSource _cts = new();
    private static readonly TimeSpan TickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);

    private readonly Dictionary<int, Channel<Command>> _playersCommands = new();
    private readonly Channel<ContextCommand> _contextCommands = Channel.CreateUnbounded<ContextCommand>();
    private readonly Core.Match.Match _match;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public MatchRuntime(Core.Match.Match match,
        ILogger logger,
        IServiceScopeFactory scopeFactory)
    {
        _match = match;
        _logger = logger;
        _scopeFactory = scopeFactory;
        foreach (var player in match.Players)
        {
            _playersCommands[player.PlayerId] = Channel.CreateBounded<Command>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = true,
            });
        }
    }

    public Player GetPlayer(int playerId) => _match.Players.First(p => p.PlayerId == playerId);

    public void Enqueue(int playerId, Command command)
    {
        if (!_playersCommands[playerId].Writer.TryWrite(command))
            _logger.LogWarning("Очередь команд матча {Id} переполнена", _match.Id);
    }

    public void EnqueueContextCommand(int playerId, int choiceNumber)
    {
        _contextCommands.Writer.TryWrite(new(playerId, choiceNumber));
    }

    private List<Command> DrainCommands()
    {
        var list = new List<Command>();
        foreach (var pair in _playersCommands)
        {
            if (pair.Value.Reader.TryRead(out var item))
                list.Add(item);
            // else
            // {
            //     var controllable = GetPlayer(pair.Key).Controllable;
            //     var target = controllable.LastCommand?.Target ?? Vector2.Zero;
            //     list.Add(new(){Controllable = controllable, Buttons = CommandButtons.None, Target = target});
            // }
        }

        return list;
    }

    private List<ContextCommand> DrainContextCommands()
    {
        var list = new List<ContextCommand>();
        while (_contextCommands.Reader.TryRead(out var item))
            list.Add(item);
        return list;
    }

    public void Start() => _loopTask = Task.Run(RunAsync);

    private async Task RunAsync()
    {
        using var timer = new PeriodicTimer(TickRate);
        var tick = 0;
        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                var stopwatch = Stopwatch.StartNew();

                var commands = DrainCommands().ToArray();
                var contextCommands = DrainContextCommands().ToArray();
                var (state, clientEvents) = _match.Step(commands, contextCommands);

                tick++;
                stopwatch.Stop();
                _logger.LogInformation($"match is running, tick {tick} took {stopwatch.ElapsedMilliseconds}ms");

                using var scope = _scopeFactory.CreateScope();
                var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
                await matchService.HandleMatchStateAsync(_match, state, clientEvents);
            }
        }
        catch (OperationCanceledException)
        {
            using var scope = _scopeFactory.CreateScope();
            var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
            await matchService.OnMatchEndingAsync(_match);
        }
        catch (Exception ex)
        {
            using var scope = _scopeFactory.CreateScope();
            var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
            await matchService.OnMatchFallAsync(_match, ex);
        }
    }

    public async ValueTask CancelAsync()
    {
        await _cts.CancelAsync();
        if (_loopTask != null) await _loopTask;
        _cts.Dispose();
    }
}