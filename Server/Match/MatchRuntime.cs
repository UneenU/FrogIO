using System.Threading.Channels;

using Core.Commands;
using Microsoft.AspNetCore.SignalR;

namespace Server.SessionManager;

public class MatchRuntime : IAsyncDisposable
{
    private readonly Core.Match _match;
    private readonly IHubContext<GameHub, IGameHub> _hubContext;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loopTask;
    private readonly Channel<(string, Command)> _commands = Channel.CreateUnbounded<(string, Command)>();


    private static readonly TimeSpan TickRate = TimeSpan.FromMilliseconds(1000.0 / 60.0);
    private const int SendEveryNTicks = 3;

    public MatchRuntime(
        Core.Match match,
        IHubContext<GameHub, IGameHub> hubContext,
        ILogger logger)
    {
        _match = match;
        _hubContext = hubContext;
        _logger = logger;
    }

    public void Enqueue(string playerId, Command command)
    {
        if (!_commands.Writer.TryWrite((playerId, command)))
            _logger.LogWarning("Очередь команд матча {Id} переполнена", _match.Id);
    }

    private List<(string, Command)> DrainCommands()
    {
        var list = new List<(string, Command)>();
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
                var commands = DrainCommands().Select(x => x.Item2).ToArray();
                var state = _match.Step(commands);
                tick++;

                if (tick % SendEveryNTicks == 0)
                {
                    await _hubContext.Clients
                        .Group(_match.Id)
                        .UpdateWorldState(state);
                }
            }
        }
        catch (OperationCanceledException)
        {
            /* нормальное завершение */
            // создание и возврат MatchResult...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Матч {Id} упал, останавливаем", _match.Id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_loopTask != null) await _loopTask;
        _cts.Dispose();
    }
}