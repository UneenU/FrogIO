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
    public float JumpPeakHeight { get; init; } = 20f;
    public int JumpCooldownTicks { get; init; } = 60;

    public float SwimSpeed { get; init; } = 0.6f;

    public int AttackCooldownTicks { get; init; } = 60;
    public float TongueLength { get; init; } = 85f;
    public float TongueSpeed { get; init; } = 4.5f;

    public static FrogConfig Default => new();
}

public static class FrogPresets
{
    public static FrogConfig Common => FrogConfig.Default;

    public static FrogConfig Heavy => FrogConfig.Default with
    {
        Name = "Heavy Frog",
        TongueLength = 120f,
        Radius = 6f,
        Mass = 600f,
        SwimSpeed = 0.6f,
        MaxJumpDistance = 60f,
        MaxWaterJumpDistance = 50f
    };

    public static FrogConfig Sticky => FrogConfig.Default with
    {
        Name = "Sticky",
        TongueLength = 70,
        Radius = 5,
        Mass = 300f,
        TongueSpeed = 3.3f
    };
}