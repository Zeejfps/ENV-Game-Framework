using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public sealed class ThemeService : IThemeService, IDisposable
{
    private readonly State<ThemeTokens> _tokens;
    private readonly Derived<StyleSheet> _sheet;

    public IReadable<ThemeTokens> Tokens => _tokens;
    public IReadable<StyleSheet> Sheet => _sheet;

    /// <summary>
    /// Constructs a theme service over <paramref name="initial"/>. The sheet is derived from
    /// the current tokens via <paramref name="buildSheet"/> — invoked once now and again on
    /// every theme swap. Tests can inject a stub builder.
    /// </summary>
    public ThemeService(ThemeTokens initial, Func<ThemeTokens, StyleSheet> buildSheet)
    {
        _tokens = new State<ThemeTokens>(initial);
        _sheet = new Derived<StyleSheet>(() => buildSheet(_tokens.Value));
    }

    public void SetTheme(ThemeTokens tokens) => _tokens.Value = tokens;

    public void Dispose()
    {
        _sheet.Dispose();
        _tokens.Dispose();
    }
}
