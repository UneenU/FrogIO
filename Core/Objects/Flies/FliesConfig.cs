namespace Core.Objects.Frogs;

public sealed record FlyConfig
{
    public const float FlyHeight = 20f;
    
    public string Name { get; init; } = "plain fly";
    public float Radius { get; init; } = 1.5f;
    public float Mass { get; init; } = 15;
    public float FlySpeed { get; init; } = 0.4f;
    public int Points { get; init; } = 1;

    public static FlyConfig Default => new();
}

public static class FlyPresets
{
    public static FlyConfig Fly => FlyConfig.Default;

    public static FlyConfig Mosquito => new FlyConfig
    {
        Name = "mosquito",
        Radius = 1.15f,
        Mass = 8f,
        Points = 2,
        FlySpeed = 0.6f
    };

    public static FlyConfig Firefly => new FlyConfig
    {
        Name = "firefly",
        Radius = 1.5f,
        Mass = 10f,
        Points = 3,
        FlySpeed = 0.3f
    };
}