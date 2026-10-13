using System.Numerics;

namespace Core.Commands;

[Flags]
public enum CommandButtons : byte
{
    None = 0,
    Move = 1 << 0,
    Attack = 1 << 1,
    All = Move | Attack
}


public readonly record struct Command(
    int ControllerId,
    Vector2 Target,
    CommandButtons Buttons)
{
    public bool Wants(CommandButtons buttons) => buttons == Buttons;
}
