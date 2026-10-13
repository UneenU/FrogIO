using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.Map;

public class Map
{
    public float Width { get; }
    public float Height { get; }
    public IList<GameObject> GameObjects { get; }
    
    private readonly List<GameObject> _pendingSpawns = new();
    private readonly List<GameObject> _pendingDespawns = new();
 
    public Map(float width, float height, IList<GameObject> gameObjects)
    {
        Width = width;
        Height = height;
        GameObjects = gameObjects;
    }
 
    public void Add(GameObject obj) => GameObjects.Add(obj);
 
    public bool Remove(GameObject obj) => GameObjects.Remove(obj);
    
    /// Добавить объект в конце тика. Безопасно звать из систем и обработчиков событий.
    public void Spawn(GameObject obj) => _pendingSpawns.Add(obj);

    /// Убрать объект в конце тика. Безопасно звать из систем и обработчиков событий.
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
            GameObjects.Remove(obj);
        foreach (var obj in _pendingSpawns)
            GameObjects.Add(obj);

        _pendingDespawns.Clear();
        _pendingSpawns.Clear();
    }
    
}