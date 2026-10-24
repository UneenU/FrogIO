using System.Collections.Concurrent;
using System.Numerics;
using Core.Commands;
using Core.Events;
using Core.Match;
using Core.Match.MatchCreator;
using Core.Objects.Abstraction;
using Microsoft.AspNetCore.SignalR;
using Server.Hub;
using Server.Match.MatchRepository;

namespace Server.Match.MatchService;

public class MatchService : IMatchService
{
    private static readonly ConcurrentDictionary<string, MatchRuntime> Runtimes = new();
    private readonly ILogger<MatchService> _logger;
    private readonly IMatchCreator _matchCreator;
    private readonly IMatchRepository _matchRepository;
    private readonly MatchNotifier _notifier;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private static readonly int SendEveryNTicks = 1;

    public MatchService(
        MatchNotifier notifier,
        ILogger<MatchService> logger, IMatchCreator matchCreator, IMatchRepository matchRepository,
        IServiceScopeFactory serviceScopeFactory)
    {
        _notifier = notifier;
        _logger = logger;
        _matchCreator = matchCreator;
        _matchRepository = matchRepository;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public (MatchInit matchInit, string matchId) StartMatch(Core.Match.MatchCreator.PlayerInfo[] playersInfo)
    {
        var match = _matchCreator.CreateMatch(playersInfo);
        var runtime = new MatchRuntime(match, _logger, _serviceScopeFactory);
        if (Runtimes.TryAdd(match.Id, runtime))
        {
            _logger.LogInformation($"Match started for {match.WorldWidth}x{match.WorldHeight}");
            runtime.Start();
        }

        foreach (var player in playersInfo)
        {
            _matchRepository.SetUserMatchAsync(player.PlayerId, match.Id);
        }

        var players = match.Players
            .Select(p => new PlayerInfo(p.PlayerId, ((GameObject)p.Controllable).Id))
            .ToArray();
        return (new MatchInit(match.WorldWidth, match.WorldHeight, players), match.Id);
    }

    public void CancelMatch(string matchId)
    {
        if (Runtimes.TryRemove(matchId, out var runtime))
        {
            _ = runtime.CancelAsync();
        }
    }

    public async Task HandleUserInputAsync(int userId, Input input)
    {
        var matchId = await _matchRepository.FindUserMatchAsync(userId);
        if (string.IsNullOrEmpty(matchId)) throw new ArgumentException("Игрок не в матче");
        var runtime = Runtimes[matchId];

        var player = runtime.GetPlayer(userId);
        var buttons = CommandButtons.None;
        foreach (var button in input.Buttons)
        {
            switch (button)
            {
                case MouseButton.Left:
                    buttons |= CommandButtons.Move;
                    break;
                case MouseButton.Right:
                    buttons |= CommandButtons.Attack;
                    break;
            }
        }

        var target = new Vector2(input.X, input.Y);

        if (input.ChoiceNumber != -1)
        {
            runtime.EnqueueContextCommand(userId, input.ChoiceNumber);
        }

        runtime.Enqueue(userId, new Command { Controllable = player.Controllable, Buttons = buttons, Target = target });
    }

    public async Task HandleMatchStateAsync(Core.Match.Match match, MatchState state, ClientEvent[] clientEvents)
    {
        foreach (var @event in clientEvents)
        {
            if (@event is PlayerGetNewLevelEvent levelUpEvent)
                await _notifier.NotifyPlayerAboutLevelUp(levelUpEvent.PlayerId, levelUpEvent.Positions);
            else throw new ArgumentException("Неожиданный тип события для клиента");
        }

        if (match.IsEnded)
        {
            await OnMatchEndingAsync(match);
        }

        if (match.Tick % SendEveryNTicks == 0)
        {
            await _notifier.NotifyPlayersAboutMatchState(match.Id, state);
        }
    }


    public async Task OnMatchCanceledAsync(Core.Match.Match match)
    {
        _logger.LogInformation($"Match {match.Id} canceled");
        await OnMatchEndingAsync(match);
    }

    public async Task OnMatchFallAsync(Core.Match.Match match, Exception ex)
    {
        _logger.LogError(ex, "Матч {Id} упал, останавливаем", match.Id);
        await OnMatchEndingAsync(match);
    }

    public async Task OnMatchEndingAsync(Core.Match.Match match)
    {
        await _notifier.NotifyPlayersAboutMatchEnding(match.Id, match.GetResult());
    }
}