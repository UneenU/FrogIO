using System.Numerics;
using Core.Objects.Frogs;

namespace Core.Objects;

public sealed class PlainFly(Vector2 position) : Fly(position, FlyPresets.Fly);

public sealed class Mosquito(Vector2 position) : Fly(position, FlyPresets.Mosquito);

public sealed class Firefly(Vector2 position) : Fly(position, FlyPresets.Firefly);
