using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;

namespace Core.MapSystem;

public record ObjectSpawn(Type Type, int Count);

public class MapConfig(float width, float height, IReadOnlyList<ObjectSpawn> spawns)
{
    public float Width { get; init; } = width;
    public float Height { get; init; } = height;

    /// <summary>Минимальный зазор между объектами.</summary>
    public float MinGap { get; init; } = 5f;

    /// <summary>Сколько случайных позиций пробовать для одного объекта.</summary>
    public int MaxAttemptsPerObject { get; init; } = 100;

    public IReadOnlyList<ObjectSpawn> Spawns { get; init; } = spawns;
}

public class MapCreator
{
    private readonly MapConfig _config;
    private readonly int _seed;
    private readonly Dictionary<Type, ICreator<GameObject>> _creators = new();

    public MapCreator(MapConfig config, int seed)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
        _seed = seed;
        RegisterCreators();
        Validate(config);
    }

    private void RegisterCreators()
    {
        _creators[typeof(WaterLily)] = new LilyCreator(_seed);
    }

    public Map Create()
    {
        var random = new Random(_seed);
        var objects = new List<GameObject>();
        Map map = new Map(_config.Width, _config.Height, objects);

        foreach (var spawn in _config.Spawns)
        {
            for (var i = 0; i < spawn.Count; i++)
            {
                var position = FindFreePosition(1, objects, random)
                               ?? throw new InvalidOperationException(
                                   $"Не удалось разместить {spawn.Type.Name} #{i + 1} из {spawn.Count}: " +
                                   $"за {_config.MaxAttemptsPerObject} попыток не найдено свободное место.");

                objects.Add(CreateInstance(map, spawn.Type, position));
            }
        }

        return new Map(_config.Width, _config.Height, objects);
    }

    private Vector2? FindFreePosition(float radius, List<GameObject> placed, Random random)
    {
        var freeWidth = _config.Width - 2 * radius;
        var freeHeight = _config.Height - 2 * radius;

        for (var attempt = 0; attempt < _config.MaxAttemptsPerObject; attempt++)
        {
            // Центр круга выбирается так, чтобы весь круг лежал внутри карты.
            var candidate = new Vector2(
                radius + random.NextSingle() * freeWidth,
                radius + random.NextSingle() * freeHeight);

            if (!IntersectsAny(candidate, radius, placed))
                return candidate;
        }

        return null;
    }

    private bool IntersectsAny(Vector2 center, float radius, List<GameObject> placed)
    {
        var gap = _config.MinGap;

        foreach (var other in placed)
        {
            var minDistance = radius + other.Radius + gap;

            // Сравниваем квадраты расстояний, чтобы не вычислять корень.
            if (Vector2.DistanceSquared(center, other.Position) < minDistance * minDistance)
                return true;
        }

        return false;
    }

    private GameObject CreateInstance(Map map, Type type, Vector2 position)
    {
        return _creators[type].Create(position, map);
    }

    private void Validate(MapConfig config)
    {
        if (config.Width <= 0 || config.Height <= 0)
            throw new ArgumentException("Размеры карты должны быть положительными.", nameof(config));

        if (config.MaxAttemptsPerObject <= 0)
            throw new ArgumentException("MaxAttemptsPerObject должен быть положительным.", nameof(config));

        foreach (var spawn in config.Spawns)
        {
            if (!_creators.ContainsKey(spawn.Type))
                throw new ArgumentException($"Для типа {spawn.Type} не нашлось креатора");

            if (spawn.Count < 0)
                throw new ArgumentException($"Отрицательное количество для {spawn.Type.Name}.", nameof(config));
        }
    }
}

public interface ICreator<out T>
{ 
    public T Create(Vector2 position, Map map);
}

public class LilyCreator(int seed) : ICreator<WaterLily>
{
    private readonly Random _random = new(seed);
    private const float LilyRadius = 15;
    private const float Dif = 6;
    
    public WaterLily Create(Vector2 position, Map map)
    {
        return new WaterLily(position, LilyRadius + Dif * _random.NextSingle());
    }
}