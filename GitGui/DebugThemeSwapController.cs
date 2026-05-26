using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

/// <summary>
/// Phase-2 acceptance hook: F12 toggles between <see cref="ThemePresets.Dark"/> and
/// <see cref="ThemePresets.Light"/>. Verifies the cascade re-resolves live across the
/// pilot panels and any open tooltip popup. Registered as a controller on <c>AppView</c>
/// so it sits on the focus queue whenever any descendant is hovered — keyboard events
/// fall through to it in the bubble phase.
/// </summary>
public sealed class DebugThemeSwapController : KeyboardMouseController
{
    private readonly IThemeService _service;
    private bool _isLight;

    public DebugThemeSwapController(IThemeService service)
    {
        _service = service;
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (e.Key != KeyboardKey.F12) return;
        _isLight = !_isLight;
        _service.SetTheme(_isLight ? ThemePresets.Light : ThemePresets.Dark);
        e.Consume();
    }
}
