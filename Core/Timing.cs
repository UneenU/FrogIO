namespace Core;

public class Time(int hours, int minutes)
{
    public int Hours { get; private set; } = hours;
    public int Minutes { get; private set; } = minutes;

    public void AddMinutes(int minutes)
    {
        Minutes += minutes;
        Hours += Minutes / 60;
        Minutes %= 60;
        if (Hours >= 24) Hours -= 24;
    }

    public static bool operator <= (Time a, Time b)
    {
        return a.Hours < b.Hours || (a.Hours == b.Hours && a.Minutes <= b.Minutes);
    }

    public static bool operator >= (Time a, Time b)
    {
        return b <= a;
    }
}