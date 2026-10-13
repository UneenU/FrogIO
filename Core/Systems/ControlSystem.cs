using System.Numerics;
using Core.Simulation;

namespace Core.Commands;

public sealed class ControlSystem(ControllerBindings bindings) : ICommandHandler
{
    public void Handle(IReadOnlyList<Command> commands, SimulationContext ctx)
    {
        foreach (var command in commands)
        {
            if (!bindings.TryGetBody(command.ControllerId, out var body))
            {
                continue;
            }

            Apply(body, command, ctx);
        }
    }
    

    /// <summary>Что значит ввод для конкретного вида тела. Новое животное = новая ветка.</summary>
    private static void Apply(IControllable body, Command command, SimulationContext ctx)
    {
        switch (body)
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

        //TODO Тут должна быть такая логика: лягушка смотрит в желаемом направлении,
        // только если она сидит на кувшинке (иначе она и так смотрит, куда ей нужно)
        if (frog.Velocity == Vector2.Zero)
            frog.Aim(direction);

        if (input.Wants(CommandButtons.Move))
        {
            bool lilyAtTarget = ctx.Map.ObjectsAtPoint<WaterLily>(input.Target).Length > 0;
            if (frog.TryStartJump(direction, direction.Length(), ctx.Tick, lilyAtTarget))
            {
                // TODO: если TryStartJump вернул true, опубликовать событие старта прыжка для клиентов.
            }
            else
            {
                frog.Swim(direction);
            }
        }

        if (input.Wants(CommandButtons.Attack))
        {
            frog.TryAttack(direction, direction.Length(), ctx.Tick);
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