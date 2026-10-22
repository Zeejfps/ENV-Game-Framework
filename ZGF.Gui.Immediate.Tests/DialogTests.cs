using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class DialogTests
{
    private readonly UiFrames _frames;
    private bool _open;
    private bool _innerOpen;
    private int _underClicks;
    private int _okClicks;
    private int _innerClicks;
    private int _escapesBehind;
    private bool _underFocused;

    public DialogTests() => _frames = new UiFrames(width: 800f, height: 600f, body: Body);

    private void Body(Ui ui)
    {
        if (ui.Input.IsKeyPressed(KeyboardKey.Escape))
            _escapesBehind++;

        var under = ui.Interact("under");
        if (under.Clicked) _underClicks++;
        if (under.Pressed) ui.Focus(under.Id, takesText: false);
        _underFocused = ui.HasFocus(under.Id);
        using (ui.Scroll("behind", new Box { Width = 800f, Height = 600f }))
        {
            ui.Leaf(under.Id, new Box { Width = Sizing.Grow(), Height = 2000f });
        }

        using (var dialog = ui.Dialog("dialog", "Settings", ref _open))
        {
            if (dialog.IsOpen)
            {
                if (ui.Button("OK")) _okClicks++;
                if (ui.Button("More")) _innerOpen = true;
                using var inner = ui.Dialog("inner", "Inner", ref _innerOpen, new Box { Width = 200f });
                if (inner.IsOpen && ui.Button("Inner OK")) _innerClicks++;
            }
        }
    }

    private void Open()
    {
        _frames.Frame();
        _open = true;
        _frames.Frame(2);
    }

    [Fact]
    public void An_open_dialog_is_centered_with_its_title_and_content()
    {
        Open();

        Assert.True(_frames.Drew("Settings"));
        Assert.True(_frames.Drew("OK"));
        var card = _frames.RectOf(ui => UiWidgets.DialogCardId(ui.GetId("dialog")));
        Assert.Equal(480f, card.Width);
        Assert.Equal(400f, card.X + card.Width / 2, 1);
        Assert.Equal(300f, card.Y + card.Height / 2, 1);
    }

    [Fact]
    public void The_backdrop_blocks_the_wheel_behind_it()
    {
        Open();

        _frames.MovePointer(20f, 20f).Frame();
        _frames.Wheel(-3).Frame(2);

        Assert.True(_open);
        Assert.Equal(0f, _frames.Run(0f, ui => ui.ScrollOffset(ui.GetId("behind")).Y));
    }

    [Fact]
    public void A_press_on_the_backdrop_closes_the_dialog_without_clicking_behind()
    {
        Open();

        _frames.Click(20f, 20f).Frame();

        Assert.False(_open);
        Assert.False(_frames.Drew("Settings"));
        Assert.Equal(0, _underClicks);
    }

    [Fact]
    public void Escape_closes_the_dialog_and_does_not_reach_what_is_behind()
    {
        Open();

        _frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.False(_open);
        Assert.Equal(0, _escapesBehind);
        Assert.False(_frames.Drew("Settings"));
    }

    [Fact]
    public void The_close_button_closes_the_dialog()
    {
        Open();

        _frames.ClickOn(ui => UiWidgets.CloseButtonId(ui.GetId("dialog")));

        Assert.False(_open);
    }

    [Fact]
    public void Content_inside_takes_clicks()
    {
        Open();

        _frames.ClickOn("OK");

        Assert.Equal(1, _okClicks);
        Assert.True(_open);
    }

    [Fact]
    public void Focus_behind_is_put_aside_while_open_and_restored_on_close()
    {
        _frames.Frame().Click(20f, 20f);
        Assert.True(_underFocused);

        _open = true;
        _frames.Frame(2);
        Assert.False(_underFocused);

        _frames.Key(KeyboardKey.Escape).Frame(3);

        Assert.True(_underFocused);
    }

    [Fact]
    public void A_dialog_opened_from_a_dialog_stacks_on_top_and_escape_closes_only_it()
    {
        Open();
        _frames.ClickOn("More").Frame(2);
        Assert.True(_frames.Drew("Inner"));

        _frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.False(_innerOpen);
        Assert.True(_open);
    }
}
