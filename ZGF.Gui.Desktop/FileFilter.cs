namespace ZGF.Gui.Desktop;

/// <summary>
/// A named group of glob patterns for <see cref="IFilePicker.PickFile"/>, e.g.
/// <c>new("Images", ["*.png", "*.jpg"])</c>. Patterns use simple wildcards only — no OS
/// dialog understands regex. macOS can only express plain <c>*.ext</c> patterns; anything
/// fancier is ignored there.
/// </summary>
public sealed record FileFilter(string Name, IReadOnlyList<string> Patterns);
