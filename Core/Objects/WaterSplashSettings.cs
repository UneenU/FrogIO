namespace Core.Objects;

public sealed class WaterSplashSettings
{
    public readonly float MaxStrength = 0.1f;
    public float StartRadius { get; }
    public float MaxRadius { get; }
    public float SpreadSpeed { get; }

    public static WaterSplashSettings DefaultWaterSplashSettings => new(3, 30, 0.4f, 60);

    public WaterSplashSettings(float startRadius, float maxRadius, float spreadSpeed, int lifetimeTicks)
    {

        StartRadius = startRadius;
        MaxRadius = maxRadius;
        SpreadSpeed = spreadSpeed;
    }
}