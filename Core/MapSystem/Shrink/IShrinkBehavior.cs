using Core.Geometry;
using Core.Systems.Abstraction;

namespace Core.MapSystem;

public enum ShrinkPhase
{
    Idle,
    Warning,
    Finished
}

/// <param name="Phase">Текущая фаза.</param>
/// <param name="TicksLeft">Тиков до следующей смены фазы (0 для <see cref="ShrinkPhase.Finished"/>).</param>
/// <param name="StepDistance">На сколько сдвигается каждая сторона за один шаг.</param>
/// <param name="Target">Границы после ближайшего шага; задано только в фазе <see cref="ShrinkPhase.Warning"/>.</param>
public readonly record struct ShrinkState(ShrinkPhase Phase, int TicksLeft, float StepDistance, Rect? Target);

public interface IShrinkBehavior
{
    ShrinkState State { get; }
    void Update(Platform platform, SimulationContext ctx);
}

public sealed class NoShrink : IShrinkBehavior
{
    public ShrinkState State => new(ShrinkPhase.Idle, 0, 0f, null);

    public void Update(Platform platform, SimulationContext ctx)
    {
    }
}
