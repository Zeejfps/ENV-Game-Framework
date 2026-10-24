using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SwitchTests
{
    private readonly UiFrames _frames;
    private bool _on;
    private bool _enabled = true;
    private int _changes;

    public SwitchTests() => _frames = new UiFrames(new DefaultUiState(), body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        if (ui.Switch("switch", ref _on, _enabled))
            _changes++;
    }

    [Fact]
    public void A_click_flips_the_switch_and_reports_the_change_once()
    {
        _frames.Frame();

        _frames.ClickOn("switch").Frame();
        Assert.True(_on);
        _frames.ClickOn("switch").Frame();

        Assert.False(_on);
        Assert.Equal(2, _changes);
    }

    [Fact]
    public void Space_flips_a_focused_switch()
    {
        _frames.Frame();
        _frames.Key(KeyboardKey.Tab).Frame();

        _frames.Key(KeyboardKey.Space).Frame();

        Assert.True(_on);
    }

    [Fact]
    public void A_switch_that_is_on_fills_with_the_accent()
    {
        _on = true;

        _frames.Frame();

        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.Accent);
    }

    [Fact]
    public void A_disabled_switch_ignores_clicks()
    {
        _enabled = false;
        _frames.Frame();

        _frames.ClickOn("switch").Frame();

        Assert.False(_on);
        Assert.Equal(0, _changes);
    }
}
