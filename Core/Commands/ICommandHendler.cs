namespace Core.Commands;

/// <summary>Первый этап тика: превращает ввод контроллеров в состояние сущностей.</summary>
public interface ICommandHandler
{
    void Handle(IReadOnlyList<Command> commands, SimulationContext ctx);
}
