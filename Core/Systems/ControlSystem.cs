using System.Numerics;
using Core.Commands;
using Core.Objects;
using Core.Objects.Abstraction;
using Core.Objects.Frogs;
using Core.Systems.Abstraction;

namespace Core.Systems;

public sealed class ControlSystem()
{
    public void Handle(IReadOnlyList<Command> commands, SimulationContext ctx)
    {
        foreach (var command in commands)
        {
            Apply(command, ctx);
        }
    }

    private static void Apply(Command command, SimulationContext ctx)
    {
        switch (command.Controllable)
        {
            case Frog frog:
                ControlFrog(frog, command, ctx);
                break;
            case Fly fly:
                ControlFly(fly, command, ctx);
                break;
        }
    }

    private static void ControlFrog(Frog frog, Command input, SimulationContext ctx)
    {
        var direction = DirectionTo(frog, input.Target);

        if (frog.Velocity == Vector2.Zero)
            frog.Aim(direction);
        
        if (input.Wants(CommandButtons.Attack))
        {
            frog.TryAttack(direction, direction.Length(), ctx.Tick);
        }

        if (input.Wants(CommandButtons.Move))
        {
            var lily = ctx.Map.ObjectsAtPoint<WaterLily>(input.Target).FirstOrDefault();
            if (lily != null)
                frog.SetTargetToMove(lily);
            else
                frog.SetTargetToMove(input.Target);
        }
    }

    private static void ControlFly(Fly fly, Command input, SimulationContext ctx)
    {
        fly.Steer(input.Wants(CommandButtons.Move) ? DirectionTo(fly, input.Target) : Vector2.Zero);
    }

    private static Vector2 DirectionTo(GameObject body, Vector2 target)
    {
        var toTarget = target - body.Position;
        return toTarget.LengthSquared() > body.Radius * body.Radius ? toTarget : Vector2.Zero;
    }
}