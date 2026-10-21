namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Ends a <see cref="UiWidgets.Screen(Ui, UiTheme)"/>: the screen's column, then its theme.</summary>
public readonly struct ScreenScope : IDisposable
{
    private readonly Ui.Scope _context;
    private readonly Ui.Scope _screen;

    internal ScreenScope(Ui.Scope context, Ui.Scope screen)
    {
        _context = context;
        _screen = screen;
    }

    public void Dispose()
    {
        _screen.Dispose();
        _context.Dispose();
    }
}
