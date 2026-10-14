using System.Numerics;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Systems;

namespace Core.Events;

public interface IEventHandler<in TEvent> where TEvent : GameEvent
{
    void Handle(TEvent e, SimulationContext simulationContext);
}

public class FrogFallInWaterEventHandler : IEventHandler<FrogFallInWaterEvent>
{
    public void Handle(FrogFallInWaterEvent e, SimulationContext simulationContext)
    {
        var frog = e.Frog;
        
        //TODO: починить id
        var splash = new WaterSplash(frog.Position, frog.Radius, 3 * frog.Radius, frog.Radius * 0.1f);
        simulationContext.Map.Spawn(splash);
    }
}

public class FrogLandOnWaterLilyEventHandler : IEventHandler<FrogLandOnWaterLilyEvent>
{
    public void Handle(FrogLandOnWaterLilyEvent e, SimulationContext simulationContext)
    {
        return;
    }
}

/// <summary>
/// Реакция на столкновение двух кругов: упругий удар вдоль нормали + раздвижка пересечения.
/// Недвижимый объект (не IMovable) ведёт себя как стена бесконечной массы.
/// </summary>
public sealed class TwoObjectsCollidedEventHandler : IEventHandler<TwoObjectsCollidedEvent>
{
    private const float Epsilon = 1e-6f;

    /// <summary>1 = абсолютно упругий удар, 0 = абсолютно неупругий.</summary>
    private const float Restitution = 1f;

    public void Handle(TwoObjectsCollidedEvent e, SimulationContext ctx)
    {
        var a = new Participant(e.Obj1);
        var b = new Participant(e.Obj2);

        var totalInvMass = a.InverseMass + b.InverseMass;
        var offset = a.Obj.Position - b.Obj.Position;
        var distance = offset.Length();
        var n = distance > Epsilon ? offset / distance : Vector2.UnitX;

        var penetration = a.Obj.Radius + b.Obj.Radius - distance;
        if (penetration > 0)
        {
            a.Movable?.MoveBy(n * (penetration * a.InverseMass / totalInvMass));
            b.Movable?.MoveBy(-n * (penetration * b.InverseMass / totalInvMass));
        }

        var approachSpeed = Vector2.Dot(a.Velocity - b.Velocity, n);
        if (approachSpeed >= 0)
            return;

        var impulse = -(1 + Restitution) * approachSpeed / totalInvMass;
        a.Movable?.AddImpulse(n * (impulse * a.InverseMass));
        b.Movable?.AddImpulse(-n * (impulse * b.InverseMass));
    }

    private readonly struct Participant(ICollidable obj)
    {
        public ICollidable Obj { get; } = obj;
        public IMovable? Movable { get; } = obj as IMovable;

        public bool IsStatic => Movable is null;
        public Vector2 Velocity => Movable?.TotalVelocity ?? Vector2.Zero;

        public float InverseMass => Movable is null ? 0f : 1f / (Obj.Radius * Obj.Radius);
    }
}

public class FrogEatFlyEventHandler : IEventHandler<FrogEatFlyEvent>
{
    public void Handle(FrogEatFlyEvent e, SimulationContext simulationContext)
    {
        simulationContext.Map.Despawn(e.Fly);
    }
}