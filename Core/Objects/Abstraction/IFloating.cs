namespace Core.Objects.Abstraction;

/// <summary>
/// Объект, привязанный к поверхности воды платформы (кувшинка, лягушка на воде или на кувшинке).
/// Если такой объект оказывается за границами платформы, он тонет.
/// </summary>
public interface IFloating
{
    bool IsAfloat { get; }
}
