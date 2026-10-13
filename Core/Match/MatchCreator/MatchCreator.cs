using System.Numerics;
using Core;
using Core.Commands;
using Core.Events;
using Core.Simulation;
using Core.Systems;

namespace Match;

public class MatchCreator : IMatchCreator
{
    private readonly MapCreator _mapCreator;
    private readonly List<PlayerInfo> _playersDtos;
    private readonly Random _random;
    private readonly AiControllerFactory _factory;
    private int _flyCount = 100;

    public MatchCreator(MapCreator mapCreator, List<PlayerInfo> playersDtos, int seed, AiControllerFactory factory)
    {
        _mapCreator = mapCreator;
        _playersDtos = playersDtos;
        _random = new Random(seed);
        _factory = factory;
    }


    public Core.Match CreateMatch()
    {
        var map = _mapCreator.Create();
        var lilies = map.Query<WaterLily>().ToList();

        var bindings = new ControllerBindings();
        List<Player> players = new();


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
        foreach (var playerDto in _playersDtos)
        {
            // создание лягушки по свойству chosenFrog
            var lily = lilies[_random.Next(0, lilies.Count)];
            var frog = new Frog(controllerId, lily.Position, 5, lily);
            map.Add(frog);

            bindings.Bind(controllerId, frog);
            var player = new Player(playerDto.playerId, frog, controllerId);
            players.Add(player);
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

        var match = new Core.Match(map, players.ToArray(), control, aiSystem, systems, events);

        return match;
    }
}

public record PlayerInfo(int playerId, string ChosenFrog);