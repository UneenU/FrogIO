using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.MapSystem;

public class Map(float width, float height, List<GameObject> gameObjects)
{
    private readonly List<GameObject> _pendingSpawns = [];
    private readonly List<GameObject> _pendingDespawns = [];
    private List<GameObject> _gameObjects = gameObjects;
    
    public float Width { get; } = width;
    public float Height { get; } = height;
    public IReadOnlyList<GameObject> GameObjects => _gameObjects.AsReadOnly();


    private void Add(GameObject obj) => _gameObjects.Add(obj);
    private bool Remove(GameObject obj) => _gameObjects.Remove(obj);
    
    public void Spawn(GameObject obj) => _pendingSpawns.Add(obj);
    public void Despawn(GameObject obj) => _pendingDespawns.Add(obj);
 
    public IEnumerable<T> Query<T>() => GameObjects.OfType<T>();
 
    public T[] ObjectsAtPoint<T>(Vector2 position) =>
        GameObjects
            .Where(o => o is T && Contains(o, position))
            .Cast<T>()
            .ToArray();

    public T[] ObjectsInCircle<T>(Vector2 position, float radius) =>
        GameObjects
            .Where(o => o is T &&  Contains(o, position, radius))
            .Cast<T>()
            .ToArray();
 
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
        foreach (var obj in _pendingDespawns)
            _gameObjects.Remove(obj);
        foreach (var obj in _pendingSpawns)
            _gameObjects.Add(obj);

        _pendingDespawns.Clear();
        _pendingSpawns.Clear();
    }
    
}