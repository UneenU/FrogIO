namespace Server.Match;

public enum MouseButton { Left = 0, Right = 2 }
public record Input(float X, float Y, MouseButton[] Buttons, int ChoiceNumber = -1);