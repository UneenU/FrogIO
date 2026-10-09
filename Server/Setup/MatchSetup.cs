using System.Numerics;
using Core;
using Core.Commands;
using Core.Events;
using Core.Simulation;
using Core.Systems;

namespace Server;

/// <summary>Всё, что нужно хосту после создания матча: сам матч, карта (для отрисовки) и игрок.</summary>
internal sealed record MatchSession(Core.Match Match, Map Map, List<Gamer> Players);

internal static class MatchSetup
{
    public const float MapWidth = 800f;
    public const float MapHeight = 400f;


    public static MatchSession Create(int seed)
    {
        var random = new Random(seed);
        var center = new Vector2(MapWidth / 2, MapHeight / 2);


        MapConfig config = new MapConfig(MapWidth, MapHeight,
            [new ObjectSpawn(typeof(WaterLily), 33),
                    new ObjectSpawn(typeof(Frog), 5),
                    new ObjectSpawn(typeof(Fly), 12)]);
        MapCreator mapCreator = new MapCreator(config, seed);
        var map = mapCreator.Create();

        // 2. Игроки и привязка контроллера к телу.
        int playerId = 1;
        List<Gamer> players = new();
        var bindings = new ControllerBindings();
        foreach (var frog in map.Query<Frog>())
        {
            var player = new Gamer(playerId, $"player {playerId}", frog);
            bindings.Bind(playerId, frog);
            players.Add(player);
            playerId++;
        }

        foreach (var fly in map.Query<Fly>())
        {
            var player = new Gamer(playerId, $"player {playerId}", fly);
            bindings.Bind(playerId, fly);
            players.Add(player);
            playerId++;
        }

        // 3. Реакции на события. Приземление в воду рождает всплеск.
        var events = new EventDispatcher();
        events.RegisterHandler(new FrogFallInWaterEventHandler());
        events.RegisterHandler(new FrogLandOnWaterLilyEventHandler());
        events.RegisterHandler(new FrogEatFlyEventHandler());
        events.RegisterHandler(new TwoObjectsCollidedEventHandler());

        // 4. Системы. Порядок = порядок выполнения внутри тика.
        ISimulationSystem[] systems =
        [
            new MovementSystem(),
            new AttackSystem(),
            new CollisionSystem(),
            new LandingSystem(),
            new WaterSplashSystem(),
        ];

        var control = new ControlSystem(bindings, []);
        var match = new Core.Match(map, players.ToArray(), control, systems, events);

        return new MatchSession(match, map, players);
    }
}