using Core.Events;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Match;

public sealed class ScoreBoard : IEventHandler<FrogEatFlyEvent>
{
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

        _scores[player.PlayerId] += e.Fly.ScorePoints;
    }

    public Dictionary<int, int> Snapshot() =>
        _players.ToDictionary(p => p.PlayerId, p => _scores[p.PlayerId]);
}
