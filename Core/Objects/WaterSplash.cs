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
    private float MaxRadius { get; } = maxRadius;
    private float SpreadSpeed { get; } = spreadSpeed;

    public float Strength
    {
        get => maxStrength * (MaxRadius - Radius) / MaxRadius;
    }

    public bool Advance()
    {
        Radius += SpreadSpeed;
        Radius = Math.Min(Radius, MaxRadius);
        return Math.Abs(Radius - MaxRadius) < 1e-6;
    }
}