namespace ZGF.ILSpy;

/// <summary>The dialogs of the screen as a whole.</summary>
public static class Dialogs
{
    /// <summary>Whether a dialog that keeps the window's shortcuts to itself is open.</summary>
    public static bool IsDialogOpen(UiState uiState)
    {
        if (uiState.Picker.IsOpen)
            return true;
        if (uiState.NuGetPicker.IsOpen)
            return true;
        if (uiState.ErrorDialog.IsOpen)
            return true;
        return uiState.ExportSetup.IsOpen;
    }

    public static void OpenAbout(UiState uiState)
    {
        uiState.IsAboutOpen = true;
    }

    public static void CloseAbout(UiState uiState)
    {
        uiState.IsAboutOpen = false;
    }
}
