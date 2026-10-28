namespace ZGF.ILSpy;

/// <summary>The dialogs of the screen as a whole.</summary>
public static class Dialogs
{
    public static void OpenAbout(UiState uiState)
    {
        uiState.IsAboutOpen = true;
    }

    public static void CloseAbout(UiState uiState)
    {
        uiState.IsAboutOpen = false;
    }
}
