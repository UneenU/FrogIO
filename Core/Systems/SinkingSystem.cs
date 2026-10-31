using Core.Events;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class SinkingSystem : IEventHandler<ObjectLeftPlatform>, ISimulationSystem
{
    private const float SinkSpeed = 1f;
    private const float DespawnDepth = -100f;

    private readonly HashSet<GameObject> _sinking = new();
    private readonly List<GameObject> _buffer = new();

    public void Handle(ObjectLeftPlatform e, SimulationContext ctx) => _sinking.Add(e.Obj);

    public void Update(SimulationContext ctx)
    {
        DetectLeavers(ctx);
        Sink(ctx);
    }

    private void DetectLeavers(SimulationContext ctx)
    {
        var platform = ctx.Map.Platform;
        foreach (var floating in ctx.Map.Query<IFloating>())
        {
            if (!floating.IsAfloat || floating is not GameObject obj) continue;
            if (_sinking.Contains(obj) || platform.Contains(obj.Position)) continue;

            ctx.Events.Publish(new ObjectLeftPlatform(obj, platform));
        }
    }

    private void Sink(SimulationContext ctx)
    {
        _buffer.Clear();
        _buffer.AddRange(_sinking);

        foreach (var obj in _buffer)
        {
            if (obj is IMovable movable)
                movable.AddImpulse(-movable.TotalVelocity);

            obj.Height -= SinkSpeed;
            if (obj.Height >= DespawnDepth) continue;

            if (obj is Frog frog)
                frog.Die();
            ctx.Map.Despawn(obj);
            _sinking.Remove(obj);
        }
    }
}
