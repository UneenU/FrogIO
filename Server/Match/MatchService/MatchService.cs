using System.Collections.Concurrent;
using Core;
using Core.Commands;
using Match;
using Microsoft.AspNetCore.SignalR;
using Server.Match;
using Server.MatchRuntime;

namespace Server.SessionManager;

public class MatchService : IMatchService
{
    private readonly ConcurrentDictionary<string, MatchRuntime> _runtimes = new();
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

    public string StartMatch(string[] playersIds)
    {
        var match = _matchCreator.CreateMatch();
        var runtime = new MatchRuntime(match, _hubContext, _logger);
        if (_runtimes.TryAdd(match.Id, runtime))
        {
            runtime.Start();
        }
        return match.Id;
    }

    public void StopMatch(string matchId)
    {
        if (_runtimes.TryRemove(matchId, out var runtime))
        {
            _ = runtime.DisposeAsync(); // не блокируем вызывающего
        }
    }

    public void HandleUserInput(string userId, Input input)
    {
        // создание объекта типа Command по пользовательскому вводу
        var command = new Command();

        var matchId = _matchRepository.FindUserMatch(userId);
        if (matchId == null) throw new ArgumentException();
        var runtime = _runtimes[matchId];
        runtime.Enqueue(userId, command);
    }
}