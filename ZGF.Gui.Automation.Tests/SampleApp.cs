
namespace ZGF.Gui.Automation.Tests;

/// <summary>The Immediate sample app, built beside these tests.</summary>
internal static class SampleApp
{
    public static string Dll => Path.Combine(AppContext.BaseDirectory, "ZGF.Gui.Immediate.Sample.dll");

    public static RemoteLaunch Launch => RemoteLaunch.OfDll(Dll) with { Arguments = ["--light"] };
}

/// <summary>A test that starts the sample app, which needs a display server on Linux.</summary>
public sealed class SampleAppFactAttribute : FactAttribute
{
    public SampleAppFactAttribute()
    {
        if (OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("DISPLAY") == null
                                      && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") == null)
            Skip = "GLFW needs a display server on Linux.";
    }
}

/// <summary>A test that starts the sample app and watches its windows through Win32, which only Windows has; elsewhere
/// the no-takeover guarantee rests on the effects log alone, which <c>A_popup_and_a_dialog_open_and_close_with_no_platform_effect_recorded</c> checks.</summary>
public sealed class SampleAppWindowsFactAttribute : FactAttribute
{
    public SampleAppWindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "No OS-level window observer here: only Win32 lets a test see every show and activation of another process's windows.";
    }
}

/// <summary>A test that runs a host running as usual on a thread of its own, which GLFW allows everywhere but macOS,
/// where it needs the process's main thread.</summary>
public sealed class LiveHostFactAttribute : FactAttribute
{
    public LiveHostFactAttribute()
    {
        Skip = new SampleAppFactAttribute().Skip;
        if (OperatingSystem.IsMacOS())
            Skip = "GLFW runs only on the process's main thread on macOS, which a test does not have.";
    }
}
