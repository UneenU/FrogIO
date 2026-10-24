using System.Numerics;
using Core.Events;
using Core.Match;
using Core.Systems.Abstraction;

namespace Core.MapSystem;

public class MapNarrowingSystem(Map map, float distanceToDelete, int narrowEveryTicks, int narrowingDuration)
{
    private float _currentWidth = map.Width;
    private float _currentHeight = map.Height;
    private int _narrowingStage = 0;
    private bool _narrowingStarted = false;


    public void Update(SimulationContext ctx)
    {
        if (ctx.Tick % narrowEveryTicks == 0 && !_narrowingStarted)
        {
            BeginNarrowing(ctx);
        }

        if (_narrowingStarted)
        {
            UpdateNarrowing(ctx);
        }
    }

    private void BeginNarrowing(SimulationContext ctx)
    {
        _narrowingStarted = true;
    }

    private void UpdateNarrowing(SimulationContext ctx)
    {
        _narrowingStage++;

        if (_narrowingStage >= narrowingDuration)
            EndNarrowing(ctx);
    }

    private void EndNarrowing(SimulationContext ctx)
    {
        _currentWidth -= 2 * distanceToDelete;
        _currentHeight -= 2 * distanceToDelete;

        var delta = new Vector2((ctx.Map.Width - _currentWidth) / 2, (ctx.Map.Height - _currentHeight) / 2);
        foreach (var gameObject in ctx.Map.GameObjects)
        {
            var position = gameObject.Position - delta;
            if (position.X < 0 || position.X > _currentWidth || position.Y < 0 || position.Y > _currentHeight)
                ctx.Events.Publish(new ObjectOutOffTheMap(gameObject));
        }

        _narrowingStage = 0;
        _narrowingStarted = false;
    }

    public MapState CurrentMapState()
    {
        return new MapState(_currentWidth, _currentHeight, _narrowingStarted, distanceToDelete);

    }
}