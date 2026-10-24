using System.Numerics;
using Core.Objects.Abstraction;

namespace Core.Commands;

public enum CommandButtons : byte
{
    None = 0,
    Move = 1 << 0,
    Attack = 1 << 1,
    StopMotion = 1 << 2,
}


public record struct Command
{
    public required IControllable Controllable { get; init; }
    public required Vector2 Target { get; init; }
    public required CommandButtons Buttons { get; init; }
    
    public bool Wants(CommandButtons buttons) => buttons == Buttons;
}
