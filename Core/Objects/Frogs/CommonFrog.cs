using System.Numerics;

namespace Core.Objects.Frogs;

public sealed class CommonFrog(
    Vector2 position,
    WaterLily? lily = null)
    : Frog(position, FrogPresets.Common, lily);