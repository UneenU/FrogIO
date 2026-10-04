using Core;
 
namespace Core.Commands;
 
/// <summary>
/// Производитель команд для NPC. Видит тот же мир, что и игрок, и отвечает той же командой.
/// ControllerId и Seq в возвращённой команде система перезапишет сама.
/// </summary>
public interface IAiController
{
    int ControllerId { get; }
 
    Command Think(GameObject body, SimulationContext ctx);
}