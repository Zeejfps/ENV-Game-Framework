using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Owns the current <see cref="ThemeTokens"/> and the <see cref="StyleSheet"/> derived from
/// them. Callers either subscribe to <see cref="Tokens"/> (canvas-draw views needing raw token
/// values) or <see cref="Sheet"/> (declarative theming via <see cref="View.StyleClasses"/> /
/// <see cref="View.StyleModifiers"/>). Theme swap goes through <see cref="SetTheme"/>; both
/// signals fire from the same source so view state stays consistent.
/// </summary>
public interface IThemeService
{
    IReadable<ThemeTokens> Tokens { get; }
    IReadable<StyleSheet> Sheet { get; }
    void SetTheme(ThemeTokens tokens);
}
