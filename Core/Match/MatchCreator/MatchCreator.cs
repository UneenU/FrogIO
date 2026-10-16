using System.Numerics;
using Core.Commands;
using Core.Events;
using Core.MapSystem;
using Core.Objects;
using Core.Objects.Frogs;
using Core.Systems;
using Core.Systems.Abstraction;

namespace Core.Match.MatchCreator;

public class MatchCreator : IMatchCreator
{
    private readonly MapCreator _mapCreator;
    private readonly Random _random;
    private readonly AiControllerFactory _factory;

    public MatchCreator(MapCreator mapCreator, int seed, AiControllerFactory factory)
    {
        _mapCreator = mapCreator;
        _random = new Random(seed);
        _factory = factory;
    }


    public Match CreateMatch(PlayerInfo[] playersInfo)
    {
        var map = _mapCreator.Create();
        Player[] players = new Player[playersInfo.Length];

        var lilies = map.Query<WaterLily>().ToList();

        List<IAiController> aiControllers = new List<IAiController>();
        foreach (var fly in map.Query<Fly>())
        {
            var aiController = _factory.NewFlyAiController(fly);
            aiControllers.Add(aiController);
        }
        var aiSystem = new AiInputSystem(aiControllers);

        // добавление лягушек
        // TODO: вынести в отдельный метод
        for (var i = 0; i < playersInfo.Length; i++)
        {
            var playerInfo = playersInfo[i];
            // создание лягушки по свойству chosenFrog
            var lily = lilies[_random.Next(0, lilies.Count)];
            Frog frog = playerInfo.ChosenFrog switch
            {
                "wren" => new CommonFrog(lily.Position, lily),
                "vex" => new HeavyFrog(lily.Position, lily),
                _ => throw new ArgumentException($"There is not such type of frog {playerInfo.ChosenFrog}")
            };
            map.Spawn(frog);

            var player = new Player(playerInfo.playerId, frog);
            players[i] = player;
        }

        map.ApplyPendingChanges();

        var control = new ControlSystem();

        // Собыийная модель
        var events = new EventDispatcher();
        events.RegisterHandler(new FrogFallInWaterEventHandler());
        events.RegisterHandler(new FrogLandOnWaterLilyEventHandler());
        events.RegisterHandler(new FrogEatFlyEventHandler());
        events.RegisterHandler(new TwoObjectsCollidedEventHandler());
        events.RegisterHandler(new StorkCaughtFrogEventHandler());

        // Системы симуляции
        ISimulationSystem[] systems =
        [
            new MovementSystem(),
            new AttackSystem(),
            new CollisionSystem(),
            new LandingSystem(),
            new WaterSplashSystem(),
            new StorkSystem(),
        ];

        //todo: выдать айди нормально
        var id = Guid.NewGuid();
        var match = new Match(id.ToString(), map, players.ToArray(), control, aiSystem, systems, events);

        return match;
    }
}

public record PlayerInfo(int playerId, string ChosenFrog);