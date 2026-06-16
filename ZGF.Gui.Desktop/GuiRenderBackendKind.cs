namespace ZGF.Gui.Desktop;

/// <summary>
/// Which graphics backend hosts the GUI. <see cref="Auto"/> picks the platform default
/// (Metal on macOS, OpenGL elsewhere). Apps that embed the GUI over their own engine
/// rendering pick the backend their engine code targets.
/// </summary>
public enum GuiRenderBackendKind
{
    Auto,
    OpenGl,
    Metal,
}
