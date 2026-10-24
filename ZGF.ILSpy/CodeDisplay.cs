using ZGF.Fonts;

namespace ZGF.ILSpy;

/// <summary>Changing how the app is drawn: the scale of the whole interface, and the font and size of
/// code and the aids drawn around it. The settings keep it with the rest of the screen.</summary>
public static class CodeDisplay
{
    public static void SetDisplayOptions(UiState uiState, DisplayOptions options)
    {
        uiState.Display = options;
    }

    /// <summary>Draws code in <paramref name="font"/>, or in the bundled font when it is null.</summary>
    public static void SetCodeFont(UiState uiState, CodeFont? font)
    {
        var chosen = uiState.Display with { Font = font };
        SetDisplayOptions(uiState, chosen);
    }

    /// <summary>Writes each type's and member's metadata token in the tree as <paramref name="format"/> says.</summary>
    public static void SetTreeTokens(UiState uiState, TokenFormat format)
    {
        var chosen = uiState.Display with { TreeTokens = format };
        SetDisplayOptions(uiState, chosen);
    }

    /// <summary>Offers the installed fonts of <paramref name="families"/> that code can be drawn in.</summary>
    public static void KeepInstalledFonts(UiState uiState, IReadOnlyList<SystemFontFamily> families)
    {
        uiState.CodeFonts.Keep(families);
    }

    /// <summary>Draws the whole interface at <paramref name="scale"/>, or the offered scale nearest it.</summary>
    public static void SetUiScale(UiState uiState, float scale)
    {
        var scaled = uiState.Display.WithUiScale(scale);
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

    /// <summary>Asks the user whether to reset the display options before <see cref="ResetDisplayOptions"/> does.</summary>
    public static void AskToResetDisplayOptions(UiState uiState)
    {
        uiState.ConfirmingDisplayReset = true;
    }

    /// <summary>Stops asking, resetting the display options when <paramref name="confirmed"/>.</summary>
    public static void FinishDisplayOptionsReset(UiState uiState, bool confirmed)
    {
        uiState.ConfirmingDisplayReset = false;
        if (confirmed)
            ResetDisplayOptions(uiState);
    }

    public static void ResetDisplayOptions(UiState uiState)
    {
        SetDisplayOptions(uiState, DisplayOptions.Default);
    }
}
