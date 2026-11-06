using System.Numerics;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

public sealed class SniperFrog(
    Vector2 position,
    WaterLily? lily = null)
    : Frog(position, FrogPresets.Sniper, lily, new SniperTongue(FrogPresets.Sniper.TongueConfig));
