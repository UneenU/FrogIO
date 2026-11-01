using System.Numerics;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

public sealed class HeavyFrog(Vector2 position, WaterLily? lily = null)
    : Frog(position, FrogPresets.Heavy, lily,
        new ShovelTongue(length: 36f, width: 30f, slowSpeed: 0.8f, strikeSpeed: 9f));
