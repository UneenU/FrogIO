using System.Numerics;
using Core.Commands;
using Core.Events;
using Core.Map;
using Core.Objects;
using Core.Systems;
using Core.Systems.Interfaces;

namespace Core.Match.MatchCreator;

public class MatchCreator : IMatchCreator
{
    private readonly MapCreator _mapCreator;
    private readonly Random _random;
    private readonly AiControllerFactory _factory;
    private int _flyCount = 100;

    public MatchCreator(MapCreator mapCreator, int seed, AiControllerFactory factory)
    {
        _mapCreator = mapCreator;
        _random = new Random(seed);
        _factory = factory;
    }


    public Match CreateMatch(PlayerInfo[] playersInfo)
    {
        var map = _mapCreator.Create();
        var bindings = new ControllerBindings();
        Player[] players = new Player[playersInfo.Length];

        var lilies = map.Query<WaterLily>().ToList();

        List<IAiController> aiControllers = new List<IAiController>();
        // добавление мух
        for (int i = 0; i < _flyCount; i++)
        {
            // создание мух через фактори
            var fly = new Fly(i, new Vector2(map.Width / 2, map.Height / 2), 2);
            map.Add(fly);

            var aiController = _factory.NewFlyAiController();
            bindings.Bind(aiController.ControllerId, fly);
            aiControllers.Add(aiController);
        }

        var aiSystem = new AiInputSystem(aiControllers, bindings);

        // добавление лягушек
        // TODO: вынести в отдельный метод
        int controllerId = 1000;
        for (var i = 0; i < playersInfo.Length; i++)
        {
            var playerInfo = playersInfo[i];
            // создание лягушки по свойству chosenFrog
            var lily = lilies[_random.Next(0, lilies.Count)];
            var frog = new Frog(controllerId, lily.Position, 5, lily);
            map.Add(frog);

            bindings.Bind(controllerId, frog);
            var player = new Player(playerInfo.playerId, frog, controllerId);
            players[i] = player;
            controllerId++;
        }

        var control = new ControlSystem(bindings);

        // Собыийная модель
        var events = new EventDispatcher();
        events.RegisterHandler(new FrogFallInWaterEventHandler());
        events.RegisterHandler(new FrogLandOnWaterLilyEventHandler());
        events.RegisterHandler(new FrogEatFlyEventHandler());
        events.RegisterHandler(new TwoObjectsCollidedEventHandler());

        // Системы симуляции
        ISimulationSystem[] systems =
        [
            new MovementSystem(),
            new AttackSystem(),
            new CollisionSystem(),
            new LandingSystem(),
            new WaterSplashSystem(),
        ];

        //todo: выдать айди нормально
        var id = Guid.NewGuid();
        var match = new Match(id.ToString(), map, players.ToArray(), control, aiSystem, systems, events);

        return match;
    }
}

public record PlayerInfo(int playerId, string ChosenFrog);