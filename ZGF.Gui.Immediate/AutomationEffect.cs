namespace ZGF.Gui.Immediate;

/// <summary>A platform effect an app in automation mode asked for, recorded instead of performed.</summary>
public abstract record AutomationEffect
{
    private AutomationEffect() { }

    /// <summary><see cref="ImmediateGuiAppHost{TUiState}.ShowWindow"/>.</summary>
    public sealed record WindowShown : AutomationEffect;

    /// <summary><see cref="ImmediateGuiAppHost{TUiState}.HideWindow"/>.</summary>
    public sealed record WindowHidden : AutomationEffect;

    /// <summary><see cref="ImmediateGuiAppHost{TUiState}.FocusWindow"/>, or a frame's <see cref="Ui.BringWindowToFront"/>.</summary>
    public sealed record BroughtToFront : AutomationEffect;

    /// <summary>A frame asked to minimize, maximize or restore the main window.</summary>
    public sealed record WindowStateRequested(WindowState State) : AutomationEffect;

    /// <summary>The pointer would have been locked to the window, or freed.</summary>
    public sealed record PointerLockChanged(bool Locked) : AutomationEffect;

    public sealed record FileOpened(string Path) : AutomationEffect;

    public sealed record FileRevealed(string Path) : AutomationEffect;

    public sealed record FolderOpened(string Path) : AutomationEffect;

    public sealed record UrlOpened(string Url) : AutomationEffect;
}
