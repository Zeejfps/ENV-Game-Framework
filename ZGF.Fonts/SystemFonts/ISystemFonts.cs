namespace ZGF.Fonts;

/// <summary>
/// The fonts installed on this machine, as the OS lists them. <see cref="NativeSystemFonts.Create"/>
/// makes the one for the current OS.
/// </summary>
public interface ISystemFonts
{
    /// <summary>
    /// Every installed family that has a face in a file on this machine, sorted by name. Blocks
    /// while the OS reads its fonts, which takes a moment with many installed, so call it off the UI
    /// thread; it is safe from any thread.
    /// </summary>
    IReadOnlyList<SystemFontFamily> ListFamilies();
}
