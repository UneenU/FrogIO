namespace Server.Match;

public record MatchInit(
    float worldWidth, float worldHeight,
    PlayerInfo[] players);

public record PlayerInfo(int userId, int frogId);
