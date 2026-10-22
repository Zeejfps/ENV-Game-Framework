using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class CheckboxTests
{
    private readonly UiFrames _frames = new();
    private bool? _group;
    private bool _single = true;
    private bool _enabled = true;
    private int _changes;

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        if (ui.Checkbox("Group", ref _group, id: "group", enabled: _enabled))
            _changes++;
        if (ui.Checkbox("Single", ref _single, id: "single", enabled: _enabled))
            _changes++;
    }

    [Fact]
    public void A_mixed_group_turns_on_then_off()
    {
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => ui.GetId("group"));
        Assert.True(_group);
        _frames.ClickOn(Body, ui => ui.GetId("group"));
        Assert.False(_group);
        _frames.ClickOn(Body, ui => ui.GetId("group"));
        Assert.True(_group);
        Assert.Equal(3, _changes);
    }

    [Fact]
    public void A_mixed_group_draws_an_accent_square_and_no_checkmark()
    {
        _single = false;
        _frames.Run(0f, Body);

        Assert.Single(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.Accent);
    }

    [Fact]
    public void A_bold_checkbox_draws_its_label_bold()
    {
        _frames.Run(0f, ui =>
        {
            using var screen = ui.Screen();
            ui.Checkbox("Plain", ref _single);
            ui.Checkbox("Changed", ref _single, bold: true);
        });

        Assert.Equal(FontWeight.Normal, _frames.Canvas.Texts.Single(t => t.Inputs.Text == "Plain").Inputs.Style.FontWeight.Value);
        Assert.Equal(FontWeight.Bold, _frames.Canvas.Texts.Single(t => t.Inputs.Text == "Changed").Inputs.Style.FontWeight.Value);
    }

    [Fact]
    public void Disabled_checkboxes_ignore_clicks()
    {
        _enabled = false;
        _frames.Run(0f, Body);

        _frames.ClickOn(Body, ui => ui.GetId("group"));
        _frames.ClickOn(Body, ui => ui.GetId("single"));

        Assert.Null(_group);
        Assert.True(_single);
        Assert.Equal(0, _changes);
    }
}
