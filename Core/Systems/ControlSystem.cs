using System.Numerics;
using Core.Simulation;

namespace Core.Commands;

/// <summary>
/// Система управления. Запоминает последнее состояние ввода каждого контроллера (игроки и ИИ
/// неразличимы) и каждый тик применяет его к телу.
/// </summary>
/// <param name="inputTimeoutTicks">Сколько тиков без новых команд ввод считается актуальным.
/// Потом кнопки сбрасываются (отключившийся клиент не должен плыть вечно).</param>
public sealed class ControlSystem(
    ControllerBindings bindings,
    IReadOnlyList<IAiController> aiControllers,
    int inputTimeoutTicks = 600) : ICommandHandler
{
    /// <summary>Кнопки-"нажатия": срабатывают один раз при переходе из отпущено в зажато.</summary>
    private const CommandButtons EdgeTriggered = CommandButtons.Attack;

    private readonly SortedDictionary<int, ControllerInput> _inputs = new();
    private readonly List<Command> _commands = new();
    private readonly List<int> _stale = new();

    /// <summary>Последний принятый Seq контроллера. Сервер отдаёт его клиенту для reconciliation.</summary>
    public bool TryGetLastProcessedSeq(int controllerId, out int seq)
    {
        var found = _inputs.TryGetValue(controllerId, out var input);
        seq = found ? input!.LastSeq : 0;
        return found;
    }

    public void Handle(IReadOnlyList<Command> playerCommands, SimulationContext ctx)
    {
        // 1. Собираем команды игроков и ИИ в один поток.
        _commands.Clear();
        _commands.AddRange(playerCommands);

        foreach (var ai in aiControllers)
        {
            if (bindings.TryGetBody(ai.ControllerId, out var aiBody))
                _commands.Add(ai.Think(aiBody, ctx) with { ControllerId = ai.ControllerId, Seq = ctx.Tick });
        }

        // 2. Обновляем запомненный ввод.
        foreach (var command in _commands)
            Receive(command, ctx);

        // 3. Применяем ввод каждого контроллера к его телу.
        foreach (var (controllerId, input) in _inputs)
        {
            if (!bindings.TryGetBody(controllerId, out var body))
            {
                _stale.Add(controllerId); // тело убрали: забываем ввод, новое тело начнёт с чистого листа
                continue;
            }

            if (ctx.Tick - input.LastReceivedTick > inputTimeoutTicks)
                input.Held = CommandButtons.None;

            Apply(body, input, ctx);
            input.Pressed = CommandButtons.None; // нажатие живёт один тик
        }

        foreach (var id in _stale)
            _inputs.Remove(id);
        _stale.Clear();
    }

    private void Receive(Command raw, SimulationContext ctx)
    {
        if (!raw.TrySanitize(out var cmd) || !bindings.TryGetBody(cmd.ControllerId, out _))
            return;

        if (!_inputs.TryGetValue(cmd.ControllerId, out var input))
        {
            input = new ControllerInput();
            _inputs[cmd.ControllerId] = input;
        }

        if (cmd.Seq <= input.LastSeq)
            return; // дубль или опоздавшая команда

        // Фронт считаем относительно предыдущего состояния, до его замены.
        input.Pressed |= cmd.Buttons & EdgeTriggered;
        input.Held = cmd.Buttons & ~EdgeTriggered;

        // Курсор не может быть за картой: так координаты остаются в разумных пределах.
        input.Target = Vector2.Clamp(cmd.Target, Vector2.Zero, new Vector2(ctx.Map.Width, ctx.Map.Height));
        input.LastSeq = cmd.Seq;
        input.LastReceivedTick = ctx.Tick;
    }

    /// <summary>Что значит ввод для конкретного вида тела. Новое животное = новая ветка.</summary>
    private static void Apply(IControllable body, ControllerInput input, SimulationContext ctx)
    {
        switch (body)
        {
            case Frog frog:
                ControlFrog(frog, input, ctx);
                break;
            case Fly fly:
                ControlFly(fly, input);
                break;
        }
    }

    private static void ControlFrog(Frog frog, ControllerInput input, SimulationContext ctx)
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

    private static void ControlFly(Fly fly, ControllerInput input)
    {
        fly.Steer(input.Wants(CommandButtons.Move) ? DirectionTo(fly, input.Target) : Vector2.Zero);
    }

    /// <summary>
    /// Вектор к точке. Если курсор внутри самого тела, возвращает ноль (мёртвая зона):
    /// иначе направление дребезжит, когда курсор лежит почти над центром.
    /// </summary>
    private static Vector2 DirectionTo(GameObject body, Vector2 target)
    {
        var toTarget = target - body.Position;
        return toTarget.LengthSquared() > body.Radius * body.Radius ? toTarget : Vector2.Zero;
    }

    private sealed class ControllerInput
    {
        public int LastSeq = int.MinValue;
        public int LastReceivedTick;
        public Vector2 Target;
        public CommandButtons Held; // что зажато сейчас
        public CommandButtons Pressed; // что нажато в этом тике (только EdgeTriggered)

        /// <summary>Активно ли действие в этом тике: уровневые кнопки по Held, нажатия по Pressed.</summary>
        public bool Wants(CommandButtons button) =>
            (((Held & ~EdgeTriggered) | Pressed) & button) == button;
    }
}