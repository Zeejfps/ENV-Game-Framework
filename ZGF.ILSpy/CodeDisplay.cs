namespace ZGF.ILSpy;

/// <summary>Changing how the documents draw code: its size and the aids drawn around it. The settings
/// keep it with the rest of the screen.</summary>
public static class CodeDisplay
{
    public static void SetDisplayOptions(UiState uiState, DisplayOptions options)
    {
        uiState.Display = options;
    }

    /// <summary>Makes code <paramref name="steps"/> font sizes larger, or smaller when negative.</summary>
    public static void ZoomCode(UiState uiState, int steps)
    {
        var zoomed = uiState.Display.Zoomed(uiState.Theme, steps);
        SetDisplayOptions(uiState, zoomed);
    }

    /// <summary>Draws code at the theme's size again.</summary>
    public static void ResetCodeZoom(UiState uiState)
    {
        var reset = uiState.Display with { FontSize = null };
        SetDisplayOptions(uiState, reset);
    }

    public static void ResetDisplayOptions(UiState uiState)
    {
        SetDisplayOptions(uiState, DisplayOptions.Default);
    }
}
