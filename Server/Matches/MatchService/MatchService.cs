using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Numerics;
using Core.Commands;
using Core.Events;
using Core.Match;
using Core.Match.MatchCreator;
using Core.Objects.Abstraction;
using Server.Data;
using Server.Match.MatchRepository;
using Server.Matches;

namespace Server.Match.MatchService;

public class MatchService(
    MatchNotifier notifier,
    ILogger<MatchService> logger,
    IMatchCreator matchCreator,
    IMatchRepository matchRepository,
    IServiceScopeFactory serviceScopeFactory,
    IRatingSystem ratingSystem,
    IUserRepository userRepository,
    IMatchHistoryRepository historyRepository)
    : IMatchService
{
    private static readonly ConcurrentDictionary<string, MatchRuntime> Runtimes = new();
    private readonly IMatchHistoryRepository _historyRepository = historyRepository;
    private static readonly int SendEveryNTicks = 1;

    public async Task<(MatchInit matchInit, string matchId)> StartMatchAsync(PlayerInit[] playersInfo)
    {
        var match = matchCreator.CreateMatch(playersInfo);
        var runtime = new MatchRuntime(match, logger, serviceScopeFactory);
        if (Runtimes.TryAdd(match.Id, runtime))
        {
            logger.LogInformation($"Match started for {match.WorldWidth}x{match.WorldHeight}");
            runtime.Start();
        }

        foreach (var player in playersInfo)
        {
            await matchRepository.SetUserMatchAsync(player.PlayerId, match.Id);
        }

        var players = match.Players
            .Select(p => new PlayerInfo(p.PlayerId, ((GameObject)p.Controllable).Id))
            .ToArray();
        return (new MatchInit(match.WorldWidth, match.WorldHeight, players), match.Id);
    }

    public async Task<MatchInit> OnRejoinMatchAsync(int userId)
    {
        var matchId = await matchRepository.FindUserMatchAsync(userId);
        if (matchId == null || !Runtimes.TryGetValue(matchId, out var runtime))
        {
            throw new ArgumentException("пользователь не может вернуться в матч: матч не существует или закончился");
        }

        var match = runtime.Match;
        var playersInfo = match.Players
            .Select(p => new PlayerInfo(p.PlayerId, ((GameObject)p.Controllable).Id))
            .ToArray();
        return new MatchInit(match.WorldWidth, match.WorldHeight, playersInfo);
    }

    public void EndMatchRuntime(string matchId)
    {
        if (Runtimes.TryRemove(matchId, out var runtime))
        {
            _ = runtime.CancelAsync();
        }
    }

    public async Task HandleUserInputAsync(int userId, Input input)
    {
        var matchId = await matchRepository.FindUserMatchAsync(userId);
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
                await notifier.NotifyPlayerAboutLevelUp(levelUpEvent.PlayerId, levelUpEvent.Positions);
            else throw new ArgumentException("Неожиданный тип события для клиента");
        }

        if (match.IsEnded)
        {
            await OnMatchEndingAsync(match);
        }

        if (match.Tick % SendEveryNTicks == 0)
        {
            await notifier.NotifyPlayersAboutMatchState(match.Id, state);
        }
    }


    public async Task OnMatchCanceledAsync(Core.Match.Match match)
    {
        logger.LogInformation($"Match {match.Id} canceled");
        await OnMatchEndingAsync(match, MatchStatus.Cancelled);
    }

    public async Task OnMatchFallAsync(Core.Match.Match match, Exception ex)
    {
        logger.LogError(ex, "Матч {Id} упал, останавливаем", match.Id);
        await OnMatchEndingAsync(match, MatchStatus.Failed);
    }

    public async Task OnMatchEndingAsync(Core.Match.Match match, MatchStatus status = MatchStatus.Completed)
    {
        try
        {
            var results = match.GetResult();
            var userIds = match.Players.Select(p => p.PlayerId).ToArray();

            var prevRating = await userRepository.GetUsersRatingAsync(userIds);
            var newRating = await ratingSystem.GetRatingChanges(results, prevRating);

            // сохранение матча в бд
            var scores = results.Scores.Values.OrderByDescending(v => v).ToArray();
            var participants = (from playerId in userIds
                let score = results.Scores[playerId]
                select new MatchParticipant()
                {
                    MatchId = match.Id,
                    UserId = playerId,
                    Place = scores.IndexOf(score) + 1,
                    Score = score,
                    RatingBefore = prevRating[playerId],
                    RatingAfter = newRating[playerId],
                }).ToList();
            var matchHistory = new GameMatch()
            {
                MatchId = match.Id,
                StartedAtUtc = DateTime.UtcNow - TimeSpan.FromSeconds((int)match.MatchDurationSeconds),
                FinishedAtUtc = DateTime.UtcNow,
                Status = status,
                Participants = participants
            };
            await _historyRepository.SaveCompletedMatchAsync(matchHistory);
            await userRepository.SetUsersRatingAsync(newRating);
            var statistic = MatchStatistic.GetStatistic(results, newRating);
            await notifier.NotifyPlayersAboutMatchEnding(match.Id, statistic);
        }
        finally
        {
            await matchRepository.RemoveMatchAsync(match.Id);
            EndMatchRuntime(match.Id);
        }
    }
}

public class MatchStatistic
{
    public Dictionary<int, UserStatistic> UserStatistics { get; }

    private MatchStatistic(Dictionary<int, UserStatistic> userStatistics)
    {
        UserStatistics = userStatistics;
    }

    public static MatchStatistic GetStatistic(MatchResult result, Dictionary<int, int> newRating)
    {
        var userStatistic = result.Scores.Keys
            .ToDictionary(id => id, id => new UserStatistic(result.Scores[id], newRating[id]));
        return new MatchStatistic(userStatistic);
    }
}

public class UserStatistic
{
    public UserStatistic(int score, int newRating)
    {
        Score = score;
        NewRating = newRating;
    }

    public int Score { get; init; }
    public int NewRating { get; init; }
}