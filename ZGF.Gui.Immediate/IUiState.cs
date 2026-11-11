namespace ZGF.Gui.Immediate;

/// <summary>What the framework's widgets read from an app's UI state. Elements take what they
/// need from it as they are recorded, so changing it mid-frame changes only what comes after.</summary>
public interface IUiState
{
    UiTheme Theme { get; }

    /// <summary>The words the framework's widgets show; English unless the app gives its own.</summary>
    UiWords Words => UiWords.English;
}
