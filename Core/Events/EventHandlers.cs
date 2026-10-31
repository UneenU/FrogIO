using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems;
using Core.Systems.Abstraction;

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
        var splash = new WaterSplash(frog.Position, frog.Radius, 3 * frog.Radius, frog.Mass * 0.1f);
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

public sealed class TwoObjectsCollidedEventHandler : IEventHandler<TwoObjectsCollidedEvent>
{
    /// <summary>1 = абсолютно упругий удар, 0 = абсолютно неупругий.</summary>
    private const float Restitution = 1f;

    public void Handle(TwoObjectsCollidedEvent e, SimulationContext ctx)
    {
        var a = new Participant(e.Obj1);
        var b = new Participant(e.Obj2);

        var totalInvMass = a.InverseMass + b.InverseMass;
        var offset = a.Obj.Position - b.Obj.Position;
        var distance = offset.Length();
        var n = distance > Constants.Epsilon ? offset / distance : Vector2.UnitX;

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

        public float InverseMass => Movable is null ? 0f : 1f / Obj.Mass;
    }
}

public class FrogEatFlyEventHandler : IEventHandler<FrogEatFlyEvent>
{
    public void Handle(FrogEatFlyEvent e, SimulationContext simulationContext)
    {
        var effect = e.Fly.Effect;
        var frog = e.Frog;
        if (effect is not null) frog.ApplyEffect(effect);
        simulationContext.Map.Despawn(e.Fly);
    }
}

public class StorkCaughtFrogEventHandler : IEventHandler<StorkCaughtFrogEvent>
{
    public void Handle(StorkCaughtFrogEvent e, SimulationContext simulationContext)
    {
        if (!e.Frog.IsAlive) return;

        e.Frog.Die();
        simulationContext.Map.Despawn(e.Frog);
    }
}