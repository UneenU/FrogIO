using System.Numerics;

namespace Core;

/// <summary>
/// Круг на воде, расширяющийся от центра. Не IMovable: позиция не меняется, меняется только радиус.
/// Сам себя с карты не убирает: Advance сообщает, что время вышло, а удаляет его система.
/// </summary>
public sealed class WaterSplash(int id, Vector2 position, WaterSplashSettings settings)
    : GameObject(id, position, settings.StartRadius)
{
    public WaterSplashSettings Settings { get; } = settings;
    public int AgeTicks { get; private set; }
    public bool IsExpired => AgeTicks >= Settings.LifetimeTicks;

    public float Strength
    {
        get => Settings.MaxStrength * (Settings.LifetimeTicks - AgeTicks) / Settings.LifetimeTicks;
    }

    /// <summary>Раз в тик: старит всплеск и расширяет круг. Возвращает true, когда время жизни вышло.</summary>
    public bool Advance()
    {
        AgeTicks++;
        Radius = Math.Min(Settings.StartRadius + Settings.SpreadSpeed * AgeTicks, Settings.MaxRadius);
        return IsExpired;
    }
}