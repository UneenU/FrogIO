using Core.Geometry;
using Core.Systems.Abstraction;

namespace Core.MapSystem;

/// <param name="StepDistance">На сколько сдвигается каждая сторона за один шаг.</param>
/// <param name="IntervalTicks">Период между началами предупреждений.</param>
/// <param name="WarningTicks">Сколько тиков длится предупреждение до самого шага.</param>
/// <param name="FirstDelayTicks">Через сколько тиков после старта матча начнётся первое предупреждение; <c>null</c> — через <paramref name="IntervalTicks"/>.</param>
/// <param name="MinSize">Минимальная ширина и высота: шаг, после которого платформа стала бы меньше, не выполняется.</param>
public sealed record StagedShrinkSettings(
    float StepDistance = 5f,
    int IntervalTicks = 17 * Constants.TicksInSecond,
    int WarningTicks = 3 * Constants.TicksInSecond,
    int? FirstDelayTicks = null,
    float MinSize = 50f)
{
    public static StagedShrinkSettings Default { get; } = new();
}

/// <summary>
/// Ступенчатое сужение: ждём → предупреждение → мгновенный шаг внутрь на каждой стороне → снова ждём.
/// Платформа сужается к своему центру.
/// </summary>
public sealed class StagedShrink : IShrinkBehavior
{
    private readonly StagedShrinkSettings _settings;
    private ShrinkPhase _phase = ShrinkPhase.Idle;
    private int _tick;
    private int _nextWarningTick;
    private int _stepTick;
    private Rect? _target;

    public StagedShrink(StagedShrinkSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.StepDistance <= 0f)
            throw new ArgumentException("StepDistance должен быть положительным.", nameof(settings));
        if (settings.IntervalTicks <= 0)
            throw new ArgumentException("IntervalTicks должен быть положительным.", nameof(settings));
        if (settings.WarningTicks < 0 || settings.WarningTicks >= settings.IntervalTicks)
            throw new ArgumentException("WarningTicks должен быть в диапазоне [0, IntervalTicks).", nameof(settings));
        if (settings.FirstDelayTicks is < 0)
            throw new ArgumentException("FirstDelayTicks не может быть отрицательным.", nameof(settings));

        _settings = settings;
        _nextWarningTick = settings.FirstDelayTicks ?? settings.IntervalTicks;
    }

    public ShrinkState State => new(_phase, TicksLeft(), _settings.StepDistance, _target);

    public void Update(Platform platform, SimulationContext ctx)
    {
        _tick = ctx.Tick;

        if (_phase == ShrinkPhase.Idle && _tick >= _nextWarningTick)
            BeginWarning(platform);

        if (_phase == ShrinkPhase.Warning && _tick >= _stepTick)
            Step(platform);
    }

    private void BeginWarning(Platform platform)
    {
        var target = platform.Bounds.Deflate(_settings.StepDistance);
        if (target.Width < _settings.MinSize || target.Height < _settings.MinSize)
        {
            _phase = ShrinkPhase.Finished;
            return;
        }

        _phase = ShrinkPhase.Warning;
        _target = target;
        _stepTick = _tick + _settings.WarningTicks;
    }

    private void Step(Platform platform)
    {
        platform.SetBounds(_target!.Value);
        _target = null;
        _phase = ShrinkPhase.Idle;
        _nextWarningTick += _settings.IntervalTicks;
    }

    private int TicksLeft() => _phase switch
    {
        ShrinkPhase.Idle => Math.Max(0, _nextWarningTick - _tick),
        ShrinkPhase.Warning => Math.Max(0, _stepTick - _tick),
        _ => 0
    };
}
