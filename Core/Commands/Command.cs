using System.Numerics;

namespace Core.Commands;

[Flags]
public enum CommandButtons : byte
{
    None = 0,
    Move = 1 << 0,    // удерживается: двигаться к Target
    Attack = 1 << 1,  // срабатывает на нажатие
    All = Move | Attack
}

/// <param name="ControllerId">Кто выдал команду. Для ИИ подставляет ControlSystem.</param>
/// <param name="Seq">Порядковый номер команды от этого контроллера. Нужен клиенту
/// для reconciliation (сервер возвращает "последняя обработанная = N") и для отсечения дублей.</param>
/// <param name="Target">Точка в мировых координатах под курсором.</param>
/// <param name="Buttons">Какие кнопки сейчас зажаты.</param>
public readonly record struct Command(
    int ControllerId,
    int Seq,
    Vector2 Target,
    CommandButtons Buttons)
{
    /// <summary>
    /// Обнуляет неизвестные биты кнопок. Возвращает false, если команду нельзя спасти
    /// (координаты NaN/Infinity): безопасной замены для точки нет, команду нужно отбросить.
    /// </summary>
    public bool TrySanitize(out Command sanitized)
    {
        sanitized = this with { Buttons = Buttons & CommandButtons.All };
        return float.IsFinite(Target.X) && float.IsFinite(Target.Y);
    }
}