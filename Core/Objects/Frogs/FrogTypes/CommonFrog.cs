using System.Numerics;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

public sealed class CommonFrog(
    Vector2 position,
    WaterLily? lily = null)
    : Frog(position, FrogPresets.Common, lily, new PlainTongue(FrogPresets.Common.TongueConfig));