using System.Numerics;
using Core.Geometry;

namespace Core.MapSystem;


public sealed class Platform
{
    public Platform(Rect bounds, IFlowField flow, IShrinkBehavior shrink, float density)
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(shrink);

        InitialBounds = bounds;
        Bounds = bounds;
        Flow = flow;
        Shrink = shrink;
        Density = density;
    }

    public Rect InitialBounds { get; }
    public Rect Bounds { get; private set; }

    public IFlowField Flow { get; }
    public float Density { get; }

    public IShrinkBehavior Shrink { get; }
    internal void SetBounds(Rect bounds) => Bounds = bounds;

    public bool Contains(Vector2 point) => Bounds.Contains(point);
}
