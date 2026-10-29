using Server.Data;

namespace Server.Matches;

public interface IMatchHistoryRepository
{
    Task SaveCompletedMatchAsync(GameMatch matchInfo);
    Task<UserMatchHistoryDto[]> GetUserMatchHistoryAsync(int userId, int pageIndex = 0, int pageSize = 10);
}

public class UserMatchHistoryDto
{
    public string MatchId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
    public MatchStatus Status { get; set; }
    public string? GameMode { get; set; }
    public int Place { get; set; }
    public int Score { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }
}