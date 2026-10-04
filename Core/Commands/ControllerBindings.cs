using System.Diagnostics.CodeAnalysis;

namespace Core.Commands;

/// <summary>
/// Кто каким телом управляет: controllerId -> тело.
/// Игроки и ИИ регистрируются одинаково. Смена тела сводится к повторному Bind.
/// При удалении тела с карты контроллер нужно отвязать (Unbind).
/// </summary>
public sealed class ControllerBindings
{
    private readonly Dictionary<int, GameObject> _bodies = new();

    public void Bind(int controllerId, GameObject body) => _bodies[controllerId] = body;

    public bool Unbind(int controllerId) => _bodies.Remove(controllerId);

    public bool TryGetBody(int controllerId, out GameObject? body) =>
        _bodies.TryGetValue(controllerId, out body);
}