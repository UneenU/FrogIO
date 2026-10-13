namespace Core.Objects;

/// <summary>
/// Параметры всплеска. Расстояния в единицах карты, время в тиках, скорость в единицах за тик.
/// Радиус растёт на SpreadSpeed за тик от StartRadius до MaxRadius и дальше не меняется;
/// всплеск живёт ровно LifetimeTicks тиков. Чтобы рост закончился вместе с жизнью,
/// задайте LifetimeTicks = ceil((MaxRadius - StartRadius) / SpreadSpeed).
/// </summary>
public sealed class WaterSplashSettings
{
    public readonly float MaxStrength = 0.1f;
    public float StartRadius { get; }
    public float MaxRadius { get; }
    public float SpreadSpeed { get; }
    public int LifetimeTicks { get; }

    public static WaterSplashSettings DefaultWaterSplashSettings => new(3, 30, 0.4f, 60);

    public WaterSplashSettings(float startRadius, float maxRadius, float spreadSpeed, int lifetimeTicks)
    {
        if (startRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(startRadius), "Начальный радиус должен быть положительным.");
        if (maxRadius < startRadius)
            throw new ArgumentOutOfRangeException(nameof(maxRadius),
                "Максимальный радиус не может быть меньше начального.");
        if (spreadSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(spreadSpeed),
                "Скорость распространения не может быть отрицательной.");
        if (lifetimeTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(lifetimeTicks), "Время жизни должно быть положительным.");

        StartRadius = startRadius;
        MaxRadius = maxRadius;
        SpreadSpeed = spreadSpeed;
        LifetimeTicks = lifetimeTicks;
    }
}