using Core.Geometry;

namespace Core.MapSystem;


public sealed record PlatformConfig
{
    public static PlatformConfig Default { get; } = new();

    /// <summary>Начальные границы платформы. <c>null</c> — вся карта.</summary>
    public Rect? Bounds { get; init; }

    public float Density { get; init; } = 1.5f;

    public Func<IFlowField> CreateFlow { get; init; } = static () => new EdgeDriftFlow();

    public Func<IShrinkBehavior> CreateShrink { get; init; } =
        static () => new StagedShrink(StagedShrinkSettings.Default);

    internal Platform Create(float worldWidth, float worldHeight)
    {
        var bounds = Bounds ?? new Rect(0, 0, worldWidth, worldHeight);

        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new ArgumentException("Размеры платформы должны быть положительными.", nameof(Bounds));

        if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > worldWidth || bounds.Bottom > worldHeight)
            throw new ArgumentException("Платформа должна целиком лежать внутри карты.", nameof(Bounds));

        return new Platform(bounds, CreateFlow(), CreateShrink(), Density);
    }
}
