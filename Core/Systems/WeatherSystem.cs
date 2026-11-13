using Core.Match;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class WeatherSystem : ISimulationSystem
{
    private readonly WeatherSettings _settings;
    private readonly Random _random;
    private readonly int _tickRate;
    private readonly (WeatherKind Kind, WeatherRule Rule)[] _rules;
    private bool _initialized;

    public WeatherSystem(WeatherSettings? settings = null, Random? random = null,
        int tickRate = Constants.TicksInSecond)
    {
        _settings = settings ?? WeatherSettings.Default;
        _random = random ?? new Random();
        _tickRate = tickRate;
        _rules =
        [
            (WeatherKind.Clear, _settings.Clear),
            (WeatherKind.Rain, _settings.Rain),
            (WeatherKind.Fog, _settings.Fog)
        ];
    }

    public void Update(SimulationContext ctx)
    {
        if (!_initialized)
        {
            _initialized = true;
            Start(_settings.Clear.Frequency > 0f ? WeatherKind.Clear : ChooseNext(null), ctx);
        }
        else if (ctx.Tick >= ctx.Map.Weather.EndsAtTick)
        {
            Start(ChooseNext(ctx.Map.Weather.Kind), ctx);
        }
    }

    private WeatherKind ChooseNext(WeatherKind? current)
    {
        var candidates = _rules.Where(pair => pair.Rule.Frequency > 0f && pair.Kind != current).ToArray();
        if (candidates.Length == 0) return current!.Value;

        var choice = _random.NextDouble() * candidates.Sum(entry => (double)entry.Rule.Frequency);
        foreach (var (kind, rule) in candidates)
        {
            choice -= rule.Frequency;
            if (choice < 0d) return kind;
        }

        return candidates[^1].Kind;
    }

    private void Start(WeatherKind kind, SimulationContext ctx)
    {
        var rule = _rules.Single(entry => entry.Kind == kind).Rule;
        var factor = 1d + (_random.NextDouble() * 2d - 1d) * _settings.DurationVariation;
        var durationTicks = Math.Max(1, (int)Math.Round(rule.MeanDurationSeconds * factor * _tickRate));
        var endsAtTick = (int)Math.Min(int.MaxValue, (long)ctx.Tick + durationTicks);
        ctx.Map.Weather = new WeatherState(kind, ctx.Tick, endsAtTick,
            kind == WeatherKind.Fog ? _settings.FogVisibilityRadius : null);
    }
}