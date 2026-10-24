namespace ZGF.ILSpy;

/// <summary>Changing how the app is drawn: the scale of the whole interface, and the size of code and
/// the aids drawn around it. The settings keep it with the rest of the screen.</summary>
public static class CodeDisplay
{
    public static void SetDisplayOptions(UiState uiState, DisplayOptions options)
    {
        uiState.Display = options;
    }

    /// <summary>Makes the whole interface <paramref name="steps"/> of the offered scales larger, or
    /// smaller when negative.</summary>
    public static void ScaleUi(UiState uiState, int steps)
    {
        var scaled = uiState.Display.Scaled(steps);
        SetDisplayOptions(uiState, scaled);
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
