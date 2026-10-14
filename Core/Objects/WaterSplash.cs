using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.Objects;

public sealed class WaterSplash(
    Vector2 position,
    float startRadius,
    float maxRadius,
    float maxStrength,
    float spreadSpeed = 0.3f)
    : GameObject(position, startRadius)
{
    public readonly float InitialStrength = maxStrength;
    public float MaxRadius { get; } = maxRadius;
    public float SpreadSpeed { get; } = spreadSpeed;

    public float Strength
    {
        get => InitialStrength * (MaxRadius - Radius) / MaxRadius;
    }

    /// <summary>Раз в тик: старит всплеск и расширяет круг. Возвращает true, когда время жизни вышло.</summary>
    public bool Advance()
    {
        Radius += SpreadSpeed;
        Radius = Math.Min(Radius, MaxRadius);
        return Math.Abs(Radius - MaxRadius) < 1e-6;
    }
}