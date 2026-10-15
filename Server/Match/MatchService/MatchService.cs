using System.Collections.Concurrent;
using System.Numerics;
using Core.Commands;
using Core.Match.MatchCreator;
using Core.Objects.Abstraction;
using Microsoft.AspNetCore.SignalR;
using Server.Hub;
using Server.Match.MatchRepository;

namespace Server.Match.MatchService;

public class MatchService : IMatchService
{
    private static readonly ConcurrentDictionary<string, MatchRuntime> Runtimes = new();
    private readonly IHubContext<GameHub, IGameHub> _hubContext;
    private readonly ILogger<MatchService> _logger;
    private readonly IMatchCreator _matchCreator;
    private readonly IMatchRepository _matchRepository;

    public MatchService(
        IHubContext<GameHub, IGameHub> hubContext,
        ILogger<MatchService> logger, IMatchCreator matchCreator, IMatchRepository matchRepository)
    {
        _hubContext = hubContext;
        _logger = logger;
        _matchCreator = matchCreator;
        _matchRepository = matchRepository;
    }

    public (MatchInit matchInit, string matchId) StartMatch(Core.Match.MatchCreator.PlayerInfo[] playersInfo)
    {
        var match = _matchCreator.CreateMatch(playersInfo);
        var runtime = new MatchRuntime(match, _hubContext, _logger);
        if (Runtimes.TryAdd(match.Id, runtime))
        {
            _logger.LogInformation($"Match started for {match.WorldWidth}x{match.WorlHeight}");
            runtime.Start();
        }

        foreach (var player in playersInfo)
        {
            _matchRepository.SetUserMatchAsync(player.playerId,  match.Id);
        }

        var players = match.Players
            .Select(p => new PlayerInfo(p.PlayerId, ((GameObject)p.Controllable).Id))
            .ToArray();
        return (new MatchInit(match.WorldWidth, match.WorlHeight, players), match.Id);
    }

    public void StopMatch(string matchId)
    {
        if (Runtimes.TryRemove(matchId, out var runtime))
        {
            _ = runtime.DisposeAsync(); // не блокируем вызывающего
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

        runtime.Enqueue(userId, new Command {Controllable = player.Controllable, Buttons = buttons, Target = target});
    }
}