using System.Runtime.InteropServices;

namespace PathsSample;

public static class Overloads
{
    public static void Take(PathsLibrary.Left.Widget widget)
    {
    }

    public static void Take(PathsLibrary.Right.Widget widget)
    {
    }
}

public class Notifier
{
    public event Action? Changed;

    public void Raise() => Changed?.Invoke();
}

public static class LeftNative
{
    [DllImport("paths-native")]
    public static extern int Answer();
}

public static class RightNative
{
    [DllImport("paths-native")]
    public static extern int Answer();
}
