namespace Core.Commands;

public interface ICommandHendler
{
    public void Handle(UserCommand[] userCommands, SimulationContext ctx);
}