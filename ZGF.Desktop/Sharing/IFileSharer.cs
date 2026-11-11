namespace ZGF.Desktop;

/// <summary>
/// Hands a file to the OS's share sheet, where the user picks an app to send it to. Made for the
/// current OS by <see cref="NativeFileSharer.Create"/>.
/// </summary>
public interface IFileSharer
{
    /// <summary>Whether this OS has a share sheet for files here. Where it does not,
    /// <see cref="Share"/> answers <see cref="ShareOutcome.Failed"/> at once.</summary>
    bool CanShare { get; }

    /// <summary>
    /// Shows the share sheet for <paramref name="file"/>, owned by the sharer's window, and runs
    /// <paramref name="onDone"/> once on the UI thread when the sheet has finished with it. Call it
    /// on the UI thread, outside a frame. The file must stay where it is until well after
    /// <paramref name="onDone"/>: the app the user picked may read it later.
    /// </summary>
    void Share(FileToShare file, Action<ShareOutcome> onDone);
}

/// <summary>A file to share: where it is, the title the share sheet shows for it, and words to send
/// with it to the apps that take them.</summary>
/// <remarks>Windows' sheet offers no way to save the file, and never asks an unpackaged app for tiles
/// of its own (<c>ShareProvidersRequested</c> is not raised), so an app that offers saving does so
/// itself, before or beside the sheet.</remarks>
public sealed record FileToShare(string Path, string Title, string? Text);

/// <summary>How a share sheet ended.</summary>
public abstract record ShareOutcome
{
    private ShareOutcome() { }

    /// <summary>The user picked an app to share it with.</summary>
    public sealed record Shared : ShareOutcome;

    /// <summary>The user closed the sheet without picking one.</summary>
    public sealed record Cancelled : ShareOutcome;

    /// <summary>No sheet could be shown, or it could not take the file.</summary>
    public sealed record Failed(string Reason) : ShareOutcome;
}
