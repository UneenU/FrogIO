namespace Server.Data;


public sealed class MatchParticipant
{
    public string MatchId { get; set; } = string.Empty;
    public int UserId { get; set; }
    
    public int Place { get; set; }
    public int Score { get; set; }
    public int RatingBefore { get; set; }
    public int RatingAfter { get; set; }

    public GameMatch Match { get; set; } = null!;
    public User User { get; set; } = null!;
}