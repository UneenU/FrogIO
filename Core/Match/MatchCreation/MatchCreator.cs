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
    private readonly MapConfig _mapConfig;
    private readonly int? _seed;
    private readonly AiControllerFactory _factory;
    private readonly WeatherSettings _weatherSettings;

    public MatchCreator(MapConfig mapConfig, int? seed, AiControllerFactory factory,
        WeatherSettings? weatherSettings = null)
    {
        _mapConfig = mapConfig;
        _seed = seed;
        _factory = factory;
        _weatherSettings = weatherSettings ?? WeatherSettings.Default;
    }


    public Match CreateMatch(PlayerInit[] playersInfo)
    {
        var random = _seed.HasValue ? new Random(_seed.Value) : new Random();
        var map = new MapCreator(_mapConfig, random.Next()).Create();
        Player[] players = new Player[playersInfo.Length];

        var lilies = map.Query<WaterLily>().ToList();

        List<IAiController> aiControllers = new List<IAiController>();
        foreach (var fly in map.Query<Fly>())
        {
            var aiController = _factory.NewFlyAiController(fly, random);
            aiControllers.Add(aiController);
        }

        var aiSystem = new AiInputSystem(aiControllers);

        // добавление лягушек
        // TODO: вынести в отдельный метод
        for (var i = 0; i < playersInfo.Length; i++)
        {
            var playerInfo = playersInfo[i];
            // создание лягушки по свойству chosenFrog
            var lily = lilies[random.Next(0, lilies.Count)];
            Frog frog = playerInfo.ChosenFrog switch
            {
                "Common Frog" => new CommonFrog(lily.Position, lily),
                "Sticky Frog" => new StickyFrog(lily.Position, lily),
                "Heavy Frog" => new HeavyFrog(lily.Position, lily),
                "Sniper Frog" => new SniperFrog(lily.Position, lily),
                _ => throw new ArgumentException($"There is not such type of frog {playerInfo.ChosenFrog}")
            };
            map.Spawn(frog);

            var player = new Player(playerInfo.PlayerId, frog);
            players[i] = player;
        }

        map.ApplyPendingChanges();

        var control = new ControlSystem();
        var levelSystem = new LevelingSystem(random);
        
        // Собыийная модель
        var events = new EventDispatcher();
        var sinkingSystem = new SinkingSystem();
        events.RegisterHandler(sinkingSystem);
        events.RegisterHandler(new FrogFallInWaterEventHandler());
        events.RegisterHandler(new FrogLandOnWaterLilyEventHandler());
        events.RegisterHandler(new FrogEatFlyEventHandler());
        events.RegisterHandler(new TwoObjectsCollidedEventHandler());
        events.RegisterHandler(new StorkCaughtFrogEventHandler());
        events.RegisterHandler(levelSystem);

        // Системы симуляции
        ISimulationSystem[] systems =
        [
            new WeatherSystem(_weatherSettings, random),
            new PlatformShrinkSystem(),
            new FlySpawnSystem(aiSystem, _factory, random: random),
            new RespawnSystem(random),
            new CollisionSystem(),
            new LandingSystem(),
            new AttackSystem(),
            new WaterSplashSystem(),
            new StorkSystem(random: random),
            sinkingSystem,
            new MovementSystem(new FlowSystem()),
            new EffectSystem()
        ];

        var id = Guid.NewGuid();
        var match = new Match(id.ToString(), map, players.ToArray(), control, levelSystem, aiSystem, systems, events);

        return match;
    }
}

public record PlayerInit(int PlayerId, string ChosenFrog);
