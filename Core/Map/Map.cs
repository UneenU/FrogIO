using System.Numerics;

namespace Core;

public class Map
{
    public float Width { get; }
    public float Height { get; }
    public IList<GameObject> GameObjects { get; }
 
    public Map(float width, float height, IList<GameObject> gameObjects)
    {
        Width = width;
        Height = height;
        GameObjects = gameObjects;
    }
 
    public void Add(GameObject obj) => GameObjects.Add(obj);
 
    public bool Remove(GameObject obj) => GameObjects.Remove(obj);
 
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

    public void Despawn(WaterSplash splash)
    {
        throw new NotImplementedException();
    }
}