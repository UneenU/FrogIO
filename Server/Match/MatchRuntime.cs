using System.Diagnostics;
using System.Threading.Channels;
using Core;
using Core.Commands;
using Core.Match;
using Microsoft.AspNetCore.SignalR;
using Server.Hub;

namespace Server.Match;

public class MatchRuntime
{
    private readonly Core.Match.Match _match;
    private readonly IHubContext<GameHub, IGameHub> _hubContext;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loopTask;
    private readonly Channel<(int, Command)> _commands = Channel.CreateUnbounded<(int, Command)>();
    private static readonly TimeSpan TickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);
    private const int SendEveryNTicks = 1;
    private readonly Dictionary<int, Player> _matchPlayers = [];

    public Player GetPlayer(int playerId) => _matchPlayers[playerId];

    public MatchRuntime(
        Core.Match.Match match,
        IHubContext<GameHub, IGameHub> hubContext,
        ILogger logger)
    {
        _match = match;
        _hubContext = hubContext;
        _logger = logger;

        foreach (var player in _match.Players)
        {
            _matchPlayers[player.PlayerId] = player;
        }
    }

    public void Enqueue(int playerId, Command command)
    {
        if (!_commands.Writer.TryWrite((playerId, command)))
            _logger.LogWarning("Очередь команд матча {Id} переполнена", _match.Id);
    }

    private List<(int, Command)> DrainCommands()
    {
        var list = new List<(int, Command)>();
        while (_commands.Reader.TryRead(out var item))
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
                
                var commands = DrainCommands().Select(x => x.Item2).ToArray();
                var state = _match.Step(commands);
                tick++;
                
                stopwatch.Stop();
                _logger.LogInformation($"match is running, tick {tick} took {stopwatch.ElapsedMilliseconds}ms");

                if (_match.IsEnded)
                {
                    await NotifyPlayersAboutMatchEnding(_match.GetResult());
                    return;
                }

                if (tick % SendEveryNTicks == 0)
                {
                    await NotifyPlayersAboutMatchState(state);
                }
            }
        }
        catch (OperationCanceledException)
        {
            await NotifyPlayersAboutMatchEnding(_match.GetResult());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Матч {Id} упал, останавливаем", _match.Id);
        }
    }

    public async ValueTask InterruptAsync()
    {
        await _cts.CancelAsync();
        if (_loopTask != null) await _loopTask;
        _cts.Dispose();
    }

    private async Task NotifyPlayersAboutMatchEnding(MatchResult result)
    {
        await _hubContext.Clients
            .Group(_match.Id)
            .MatchEnded(result);
    }

    private async Task NotifyPlayersAboutMatchState(MatchState state)
    {
        await _hubContext.Clients
            .Group(_match.Id)
            .UpdateWorldState(state);
    }
}