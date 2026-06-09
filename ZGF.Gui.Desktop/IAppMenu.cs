namespace ZGF.Gui.Desktop;

/// <summary>
///     Platform hook for installing a native application menu bar that GLFW does not
///     expose. Only macOS has a real menu bar (the global bar at the top of the screen);
///     the no-op implementation covers Windows/Linux, where this is conventionally absent.
///     The application layer composes a platform-agnostic <see cref="AppMenuBar"/>; the
///     implementation maps it to the OS-native structure (AppKit's NSMenu on macOS).
/// </summary>
public interface IAppMenu
{
    /// <summary>
    ///     Installs <paramref name="menuBar"/> as the application's menu bar, replacing any
    ///     menu GLFW created by default. Call once after the main window exists.
    /// </summary>
    void Install(AppMenuBar menuBar);
}

/// <summary>Keyboard-equivalent modifier flags, mapped to the platform's native masks.</summary>
[Flags]
public enum AppMenuModifiers
{
    None = 0,
    Command = 1,
    Shift = 2,
    Option = 4,
    Control = 8,
}

/// <summary>
///     Role hints that let the platform wire a menu into OS-managed slots — e.g. macOS
///     tracks the Window and Services menus specially (window list, app services).
/// </summary>
public enum AppMenuRole
{
    Standard,
    Application,
    Window,
    Services,
    Help,
}

/// <summary>
///     Built-in actions the OS already implements (Quit, Hide, Minimize, …). The platform
///     binds these to the native first-responder selector, so no callback is needed. Custom
///     behavior uses <see cref="AppMenuItem.OnClick"/> instead.
/// </summary>
public enum AppMenuStandardAction
{
    None,
    About,
    Hide,
    HideOthers,
    ShowAll,
    Quit,
    Minimize,
    Zoom,
    Close,
    BringAllToFront,
    ToggleFullScreen,
}

public sealed class AppMenuItem
{
    public string Title { get; init; } = "";
    public bool IsSeparator { get; init; }

    /// <summary>Custom handler, invoked on the UI thread when the item is chosen.</summary>
    public Action? OnClick { get; init; }

    /// <summary>OS-provided behavior; ignored when <see cref="OnClick"/> is set.</summary>
    public AppMenuStandardAction Standard { get; init; } = AppMenuStandardAction.None;

    /// <summary>Single-character key equivalent (e.g. "q"); empty for none.</summary>
    public string KeyEquivalent { get; init; } = "";

    public AppMenuModifiers Modifiers { get; init; } = AppMenuModifiers.Command;

    public static AppMenuItem Separator => new() { IsSeparator = true };
}

public sealed class AppMenu
{
    public string Title { get; init; } = "";
    public AppMenuRole Role { get; init; } = AppMenuRole.Standard;
    public List<AppMenuItem> Items { get; init; } = new();
}

public sealed class AppMenuBar
{
    public List<AppMenu> Menus { get; init; } = new();
}
