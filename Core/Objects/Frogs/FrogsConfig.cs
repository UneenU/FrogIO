using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

public sealed record FrogConfig
{
    public string Name { get; init; } = "Common Frog";
    public float Radius { get; init; } = 4f;
    public float Hp { get; init; } = 100f;

    public float Mass { get; init; } = 300f;

    public float RadiusPerMass { get; init; } = 0.004f;

    public float JumpSpeed { get; init; } = 1.5f;
    public float MaxJumpDistance { get; init; } = 100f;
    public float MaxWaterJumpDistance { get; init; } = 70f;
    public float JumpPeakHeight { get; init; } = 15f;
    public int JumpCooldownTicks { get; init; } = 60;

    public float SwimSpeed { get; init; } = 0.6f;

    public int AttackCooldownTicks { get; init; } = 60;
    public TongueConfig TongueConfig { get; init; } = new TongueConfig(90, 2.5f,4f);

    public static FrogConfig Default => new();
}

public static class FrogPresets
{
    public static FrogConfig Common => FrogConfig.Default;

    public static FrogConfig Sniper => FrogConfig.Default with
    {
        Name = "Sniper Frog",
        TongueConfig = new TongueConfig(150, 2, 8),
        Radius = 3.5f,
        Mass = 180f,
        MaxJumpDistance = 70f,
        MaxWaterJumpDistance = 45f,
        JumpCooldownTicks = 90,
        SwimSpeed = 0.4f
    };

    public static FrogConfig Heavy => FrogConfig.Default with
    {
        Name = "Heavy Frog",
        TongueConfig = new TongueConfig(40, 20, 5f),
        Radius = 6f,
        Mass = 600f,
        SwimSpeed = 0.6f,
        MaxJumpDistance = 60f,
        MaxWaterJumpDistance = 50f
    };

    public static FrogConfig Sticky => FrogConfig.Default with
    {
        Name = "Sticky Frog",
        Radius = 5,
        Mass = 300f,
        TongueConfig = new TongueConfig(65, 2, 4)
    };
}
