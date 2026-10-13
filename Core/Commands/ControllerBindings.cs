using System.Diagnostics.CodeAnalysis;
using Core.Objects.Abstraction;

namespace Core.Commands;


public sealed class ControllerBindings
{
    private readonly Dictionary<int, IControllable> _bodies = new();

    public void Bind(int controllerId, IControllable body) => _bodies[controllerId] = body;

    public bool Unbind(int controllerId) => _bodies.Remove(controllerId);

    public bool TryGetBody(int controllerId, out IControllable? body) =>
        _bodies.TryGetValue(controllerId, out body);
}