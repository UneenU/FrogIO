namespace Core.Objects.Frogs;

public sealed record FlyConfig
{
    public const float FlyHeight = 10f;
    
    public string Name { get; init; } = "plain fly";
    public float Radius { get; init; } = 3.5f;
    public float Mass { get; init; } = 30;
    public float FlySpeed { get; init; } = 0.5f;
    public int Points { get; init; } = 1;

    public static FlyConfig Default => new();
}

public static class FlyPresets
{
    public static FlyConfig Fly => FlyConfig.Default;

    public static FlyConfig Mosquito => new FlyConfig
    {
        Name = "mosquito",
        Radius = 2f,
        Mass = 20f,
        Points = 2,
        FlySpeed = 0.7f
    };
}