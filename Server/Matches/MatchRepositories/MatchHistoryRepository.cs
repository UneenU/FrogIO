using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Storage;

namespace Server.Matches;

public class MatchHistoryRepository(GameDbContext dbContext) : IMatchHistoryRepository
{
    public async Task SaveCompletedMatchAsync(GameMatch matchInfo)
    {
        await dbContext.Matches.AddAsync(matchInfo);
        await dbContext.SaveChangesAsync();
    }

    public async Task<UserMatchHistoryDto[]> GetUserMatchHistoryAsync(int userId, int pageIndex = 0, int pageSize = 10)
    {
        return await dbContext.Set<MatchParticipant>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Match.StartedAtUtc)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(p => new UserMatchHistoryDto
            {
                MatchId = p.MatchId,
                StartedAtUtc = p.Match.StartedAtUtc,
                FinishedAtUtc =  p.Match.FinishedAtUtc,
                Status = p.Match.Status,
                GameMode = p.Match.GameMode,
                Place = p.Place,
                Score = p.Score,
                RatingBefore = p.RatingBefore,
                RatingAfter = p.RatingAfter
            })
            .ToArrayAsync();
    }
}