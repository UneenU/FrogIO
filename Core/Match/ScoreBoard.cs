using Core.Events;
using Core.Systems;

namespace Core.Match;

public sealed class ScoreBoard : IEventHandler<FrogEatFlyEvent>
{
    private const int PointsPerFly = 1;

    private readonly Player[] _players;
    private readonly Dictionary<int, int> _scores = new();

    public ScoreBoard(Player[] players)
    {
        _players = players;
        foreach (var player in players)
            _scores[player.PlayerId] = 0;
    }

    public void Handle(FrogEatFlyEvent e, SimulationContext ctx)
    {
        // Лягушки без игрока (NPC) очков не получают.
        var player = _players.FirstOrDefault(p => ReferenceEquals(p.Controllable, e.Frog));
        if (player is null) return;

        _scores[player.PlayerId] += PointsPerFly;
    }

    public PlayerScore[] Snapshot() =>
        _players.Select(p => new PlayerScore(p.PlayerId, _scores[p.PlayerId])).ToArray();
}
