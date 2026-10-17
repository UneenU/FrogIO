using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.MapSystem;

public class Map
{
    private int _availableId = 1;
    private readonly List<GameObject> _pendingSpawns;
    private readonly List<GameObject> _pendingDespawns = [];
    private readonly List<GameObject> _gameObjects = [];

    // Кэш результатов Query<T>: тип -> IReadOnlyList<T>.
    // Сбрасывается при любом реальном изменении состава объектов.
    private readonly Dictionary<Type, object> _queryCache = [];

    public Map(float width, float height, List<GameObject> gameObjects)
    {
        Width = width;
        Height = height;

        // Копируем, чтобы ApplyPendingChanges не очищал список вызывающего кода
        _pendingSpawns = [.. gameObjects];
        GameObjects = _gameObjects.AsReadOnly(); // живое представление, создаём один раз
        ApplyPendingChanges();
    }

    public float Width { get; }
    public float Height { get; }
    public IReadOnlyList<GameObject> GameObjects { get; }

    public void Spawn(GameObject obj) => _pendingSpawns.Add(obj);
    public void Despawn(GameObject obj) => _pendingDespawns.Add(obj);

    /// <summary>
    /// Возвращает закэшированный список объектов типа T.
    /// Кэш действителен до следующего вызова <see cref="ApplyPendingChanges"/>.
    /// </summary>
    public IReadOnlyList<T> Query<T>()
    {
        var type = typeof(T);

        if (_queryCache.TryGetValue(type, out var cached))
            return (IReadOnlyList<T>)cached;

        var list = new List<T>();
        foreach (var obj in _gameObjects)
        {
            if (obj is T typed)
                list.Add(typed);
        }

        var result = list.AsReadOnly();
        _queryCache[type] = result;
        return result;
    }

    public T[] ObjectsAtPoint<T>(Vector2 position)
    {
        var result = new List<T>();
        foreach (var item in Query<T>())
        {
            if (item is GameObject obj && Contains(obj, position))
                result.Add(item);
        }
        return result.ToArray();
    }

    public T[] ObjectsInCircle<T>(Vector2 position, float radius)
    {
        var result = new List<T>();
        foreach (var item in Query<T>())
        {
            if (item is GameObject obj && Contains(obj, position, radius))
                result.Add(item);
        }
        return result.ToArray();
    }

    private static bool Contains(GameObject obj, Vector2 point) =>
        Vector2.Distance(point, obj.Position) <= obj.Radius;

    private static bool Contains(GameObject obj, Vector2 point, float radius) =>
        Vector2.Distance(point, obj.Position) <= radius + obj.Radius;

    public float GetDensityAtPosition(Vector2 position)
    {
        //TODO: реализовать логику определения плотности среды в точке
        return 1.5f;
    }

    public void ApplyPendingChanges()
    {
        if (_pendingDespawns.Count == 0 && _pendingSpawns.Count == 0)
            return; // ничего не изменилось — кэш остаётся валидным

        if (_pendingDespawns.Count > 0)
        {
            var toRemove = new HashSet<GameObject>(_pendingDespawns);
            _gameObjects.RemoveAll(toRemove.Contains); // один проход вместо O(n*m)
        }

        foreach (var obj in _pendingSpawns)
        {
            obj.Id = _availableId++;
            _gameObjects.Add(obj);
        }

        _pendingDespawns.Clear();
        _pendingSpawns.Clear();
        _queryCache.Clear();
    }
}