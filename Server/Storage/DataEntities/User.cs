namespace Server.Data;

public sealed class User
{
    public int UserId { get; set; }

    public string Nickname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int Rating { get; set; } = 1000;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MatchParticipant> MatchParticipants { get; set; }
        = new List<MatchParticipant>();
}