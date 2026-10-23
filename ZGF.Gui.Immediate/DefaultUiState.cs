namespace ZGF.Gui.Immediate;

/// <summary>A UI state holding only the theme, for a UI that needs nothing else.</summary>
public sealed class DefaultUiState : IUiState
{
    public UiTheme Theme { get; set; } = UiTheme.Dark;
}
