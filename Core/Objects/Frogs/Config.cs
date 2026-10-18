namespace Core.Objects.Frogs;

public sealed record FrogConfig
{
    public string Name { get; init; } = "Common Frog";
    public float Radius { get; init; } = 4f;

    // --- Физика ---
    public float Mass { get; init; } = 300f;

    /// <summary>Прирост радиуса на единицу массы</summary>
    public float RadiusPerMass { get; init; } = 0.01f;

    // --- Прыжки ---
    public float JumpSpeed { get; init; } = 1.5f;
    public float MaxJumpDistance { get; init; } = 100f;
    public float MaxWaterJumpDistance { get; init; } = 70f;
    public float JumpPeakHeight { get; init; } = 20f;
    public int JumpCooldownTicks { get; init; } = 60;

    // --- Плавание ---
    public float SwimSpeed { get; init; } = 0.8f;

    // --- Атака ---
    public int AttackCooldownTicks { get; init; } = 60;
    public float TongueLength { get; init; } = 80f;
    public float TongueSpeed { get; init; } = 3.5f;

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
}