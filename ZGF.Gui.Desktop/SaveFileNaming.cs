namespace ZGF.Gui.Desktop;

/// <summary>
/// The one rule every <see cref="IFilePicker.PickSaveFile"/> implementation applies to what the
/// user typed, so the same keystrokes give the same path on every platform.
/// </summary>
/// <remarks>
/// The platforms disagree about extensions. The Windows dialog appends its default extension,
/// macOS's <c>choose file name</c> appends nothing at all, and zenity depends on the desktop.
/// Rather than let a caller discover that, each implementation runs the result through here — on
/// Windows it is a no-op because the dialog already did it, which is exactly the point.
/// </remarks>
internal static class SaveFileNaming
{
    /// <summary>
    /// Gives <paramref name="chosenPath"/> the extension of <paramref name="suggestedFileName"/>
    /// when the user typed a name without one. A name that already has an extension is left
    /// alone: typing <c>notes.txt</c> is a decision, not an omission.
    /// </summary>
    public static string ApplyDefaultExtension(string chosenPath, string? suggestedFileName)
    {
        if (string.IsNullOrEmpty(chosenPath) || string.IsNullOrEmpty(suggestedFileName))
            return chosenPath;

        var extension = Path.GetExtension(suggestedFileName);
        if (string.IsNullOrEmpty(extension))
            return chosenPath;

        // A trailing dot is the user asking for no extension at all, which is theirs to ask for;
        // HasExtension is false for it, so it has to be caught before the append.
        if (Path.HasExtension(chosenPath) || chosenPath.EndsWith('.'))
            return chosenPath;

        return chosenPath + extension;
    }
}
