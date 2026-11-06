using System.Numerics;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

public sealed class HeavyFrog(Vector2 position, WaterLily? lily = null)
    : Frog(position, FrogPresets.Heavy, lily, new ShovelTongue(FrogPresets.Heavy.TongueConfig));
