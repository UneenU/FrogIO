using System.Numerics;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

public sealed class StickyFrog(
    Vector2 position,
    WaterLily? lily = null)
    : Frog(position, FrogPresets.Heavy, lily, new StickyTongue(100, 5, 3));