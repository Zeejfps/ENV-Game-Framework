using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class DialogActionsTests
{
    private readonly UiFrames _frames;
    private bool _open = true;
    private bool _closeButton;

    public DialogActionsTests() => _frames = new UiFrames(width: 800f, height: 600f, body: Body);

    private void Body(Ui ui)
    {
        using var dialog = ui.Dialog("confirm", "Reset", ref _open, closeButton: _closeButton);
        if (!dialog.IsOpen) return;
        ui.Text("Are you sure?");
        using (ui.DialogActions())
        {
            ui.Button("Cancel");
            ui.Button("Reset it");
        }
    }

    private float CardHeight() => _frames.Frame(2).RectOf(ui => UiWidgets.DialogCardId(ui.GetId("confirm"))).Height;

    [Fact]
    public void Actions_sit_at_the_end_set_apart_from_the_content_by_the_dialog_gap()
    {
        _frames.Frame(2);
        var theme = UiTheme.Dark;
        var text = _frames.Canvas.Texts.Single(t => t.Inputs.Text == "Are you sure?").Inputs.Position;
        var cancel = _frames.TextRect("Cancel");
        var reset = _frames.TextRect("Reset it");

        Assert.True(cancel.X < reset.X);
        var button = _frames.Canvas.Rects.Select(r => r.Inputs).Where(r => r.Style.BackgroundColor == theme.Control).Max(r => r.Position.Top);
        var gap = text.Top - text.Height - button;
        Assert.True(gap >= theme.DialogGap, $"the buttons start {gap} below the text");
    }

    [Fact]
    public void Leaving_off_the_close_button_makes_the_title_row_no_taller_than_its_title()
    {
        var without = CardHeight();
        _closeButton = true;
        var with = CardHeight();

        Assert.True(without < with, $"{without} without the close button, {with} with it");
    }

    [Fact]
    public void Escape_still_closes_a_dialog_without_a_close_button()
    {
        _frames.Frame().Key(KeyboardKey.Escape).Frame();

        Assert.False(_open);
    }
}
