namespace Server.Data;

public sealed class GameMatch
{
    public string MatchId { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public DateTime FinishedAtUtc { get; set; }

    public MatchStatus Status { get; set; }

    public string? GameMode { get; set; } = "default";

    public ICollection<MatchParticipant> Participants { get; set; }
        = new List<MatchParticipant>();
}


public enum MatchStatus
{
    Completed,
    Cancelled,
    Failed
}