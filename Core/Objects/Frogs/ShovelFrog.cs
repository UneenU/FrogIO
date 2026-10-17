using System.Numerics;
using Core.Objects.Frogs.Tongues;

namespace Core.Objects.Frogs;

/// <summary>Лягушка с широким языком-лопатой: короткая дистанция и массовый захват хлопком.</summary>
public sealed class ShovelFrog(Vector2 position, WaterLily? lily = null)
    : Frog(position, FrogPresets.Common, lily,
        new ShovelTongue(length: 36f, width: 30f, slowSpeed: 0.8f, strikeSpeed: 9f));
