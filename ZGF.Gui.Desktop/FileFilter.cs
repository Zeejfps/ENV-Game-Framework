namespace ZGF.Gui.Desktop;

/// <summary>
/// A named group of glob patterns for <see cref="IFilePicker.PickFile"/>, e.g.
/// <c>new("Images", ["*.png", "*.jpg"])</c>. Patterns use simple wildcards only — no OS
/// dialog understands regex. macOS filters by Uniform Type Identifier, so it can only express
/// plain <c>*.ext</c> patterns whose extension it has a UTI for; anything else is ignored there
/// (and logged). The <see cref="Name"/> shows up as a dropdown entry on Windows and Linux only —
/// macOS has no filter dropdown and instead greys out every file that doesn't match.
/// </summary>
public sealed record FileFilter(string Name, IReadOnlyList<string> Patterns);
