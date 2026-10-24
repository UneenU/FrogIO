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
    private readonly Dictionary<Type, object> _queryCache = [];

    public float Width { get; }
    public float Height { get; }
    public IReadOnlyList<GameObject> GameObjects => _gameObjects.AsReadOnly();
    
    
    public Map(float width, float height, List<GameObject> gameObjects)
    {
        Width = width;
        Height = height;
        _pendingSpawns = [.. gameObjects];
        ApplyPendingChanges();
    }

    public void Spawn(GameObject obj) => _pendingSpawns.Add(obj);
    public void Despawn(GameObject obj) => _pendingDespawns.Add(obj);


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

    public T[] ObjectsAtPoint<T>(Vector2 point)
    {
        var result = new List<T>();
        foreach (var item in Query<T>())
        {
            if (item is GameObject obj && Contains(obj, point))
                result.Add(item);
        }

        return result.ToArray();
    }

    public T[] ObjectsInCircle<T>(Vector2 center, float radius)
    {
        var result = new List<T>();
        foreach (var item in Query<T>())
        {
            if (item is GameObject obj && Contains(obj, center, radius))
                result.Add(item);
        }

        return result.ToArray();
    }

    private static bool Contains(GameObject obj, Vector2 point) =>
        Vector2.Distance(point, obj.Position) <= obj.Radius;

    private static bool Contains(GameObject obj, Vector2 point, float radius) =>
        Vector2.Distance(point, obj.Position) <= radius + obj.Radius;

    public float GetDensityAtPosition(Vector2 point) => 1.5f;

    public void ApplyPendingChanges()
    {
        if (_pendingDespawns.Count == 0 && _pendingSpawns.Count == 0)
            return;

        if (_pendingDespawns.Count > 0)
        {
            var toRemove = new HashSet<GameObject>(_pendingDespawns);
            _gameObjects.RemoveAll(toRemove.Contains);
        }

        foreach (var obj in _pendingSpawns)
        {
            if (obj.Id == -1)
                obj.Id = _availableId++;
            _gameObjects.Add(obj);
        }

        _pendingDespawns.Clear();
        _pendingSpawns.Clear();
        _queryCache.Clear();
    }
}