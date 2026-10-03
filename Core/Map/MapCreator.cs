using System.Numerics;

namespace Core;

public record ObjectSpawn(Type Type, int Count);

public class MapConfig
{
    public float Width { get; init; }
    public float Height { get; init; }

    /// <summary>Минимальный зазор между объектами.</summary>
    public float MinGap { get; init; } = 5f;

    /// <summary>Сколько случайных позиций пробовать для одного объекта.</summary>
    public int MaxAttemptsPerObject { get; init; } = 100;

    public IReadOnlyList<ObjectSpawn> Spawns { get; init; } = [];
}

public class MapCreator
{
    private readonly MapConfig _config;
    private readonly int _seed;

    public MapCreator(MapConfig config, int seed)
    {
        ArgumentNullException.ThrowIfNull(config);
        Validate(config);
        _config = config;
        _seed = seed;
    }

    public Map Create()
    {
        var random = new Random(_seed);
        var objects = new List<GameObject>();

        // Крупные объекты ставим первыми: их сложнее разместить, когда карта уже заполнена.
        var plan = _config.Spawns
            .Select(spawn => (Spawn: spawn, Radius: ProbeSize(spawn.Type)))
            .OrderByDescending(x => x.Radius)
            .ToList();

        foreach (var (spawn, radius) in plan)
        {
            if (2*radius > _config.Width || 2*radius > _config.Height)
                throw new InvalidOperationException(
                    $"Объект {spawn.Type.Name} ({radius}) не помещается в карту " +
                    $"{_config.Width}x{_config.Height}.");

            for (var i = 0; i < spawn.Count; i++)
            {
                var position = FindFreePosition(radius, objects, random)
                    ?? throw new InvalidOperationException(
                        $"Не удалось разместить {spawn.Type.Name} #{i + 1} из {spawn.Count}: " +
                        $"за {_config.MaxAttemptsPerObject} попыток не найдено свободное место.");

                objects.Add(CreateInstance(spawn.Type, position));
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

    /// <summary>Создаёт пробный экземпляр, чтобы узнать размер типа до размещения.</summary>
    private static float ProbeSize(Type type) => CreateInstance(type, Vector2.Zero).Radius;

    private static GameObject CreateInstance(Type type, Vector2 position)
    {
        try
        {
            return (GameObject)Activator.CreateInstance(type, position)!;
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException(
                $"У типа {type.Name} нет публичного конструктора с единственным параметром Vector2 position.");
        }
    }

    private static void Validate(MapConfig config)
    {
        if (config.Width <= 0 || config.Height <= 0)
            throw new ArgumentException("Размеры карты должны быть положительными.", nameof(config));

        if (config.MaxAttemptsPerObject <= 0)
            throw new ArgumentException("MaxAttemptsPerObject должен быть положительным.", nameof(config));

        foreach (var spawn in config.Spawns)
        {
            if (spawn.Count < 0)
                throw new ArgumentException($"Отрицательное количество для {spawn.Type.Name}.", nameof(config));

            if (!typeof(GameObject).IsAssignableFrom(spawn.Type) || spawn.Type.IsAbstract)
                throw new ArgumentException(
                    $"{spawn.Type.Name} должен быть неабстрактным наследником GameObject.", nameof(config));
        }
    }
}