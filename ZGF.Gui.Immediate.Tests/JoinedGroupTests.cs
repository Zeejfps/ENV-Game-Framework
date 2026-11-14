using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class JoinedGroupTests
{
    private static readonly IconGlyph Glyph = IconGlyph.FromFont("x", "icons");

    [Fact]
    public void Its_controls_sit_side_by_side_with_no_gap_inside_its_outline()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.JoinedGroup("group"))
            {
                ui.IconButton("first", Glyph);
                ui.IconButton("second", Glyph);
            }
        }).Frame(2);
        var group = frames.RectOf("group");
        var first = frames.RectOf("first");
        var second = frames.RectOf("second");

        Assert.Equal(first.Right, second.X, 0.5f);
        Assert.Equal(group.X + UiTheme.Dark.BorderWidth, first.X, 0.5f);
        Assert.Equal(group.Height - UiTheme.Dark.BorderWidth * 2f, first.Height, 0.5f);
    }

    [Fact]
    public void TakeJoinedSlot_says_first_then_after_inside_a_group_and_null_outside()
    {
        var slots = new List<JoinedSlot?>();
        new UiFrames(new DefaultUiState(), body: ui =>
        {
            slots.Clear();
            using var screen = ui.Screen();
            slots.Add(ui.TakeJoinedSlot());
            using (ui.JoinedGroup("group"))
            {
                slots.Add(ui.TakeJoinedSlot());
                slots.Add(ui.TakeJoinedSlot());
            }
        }).Frame();

        Assert.Equal([null, new JoinedSlot(true), new JoinedSlot(false)], slots);
    }

    [Fact]
    public void An_icon_toggle_flips_its_value_when_clicked_and_says_it_is_checked()
    {
        var on = false;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            ui.IconToggle("toggle", Glyph, ref on, "Toggle");
        }).Frame();

        frames.ClickOn("toggle").Frame();

        Assert.True(on);
        Assert.True(frames.Canvas.Rects.Any(r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.Selection));
    }

    [Fact]
    public void A_number_fields_label_shows_inside_it_before_its_value()
    {
        var value = 3;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            ui.NumberField("start", ref value, 1, 9, 120f, label: "Start");
        }).Frame(2);

        var field = frames.RectOf("start");
        var label = frames.TextRect("Start");
        var number = frames.TextRect("3");

        Assert.True(label.X > field.X && label.Right <= number.X && number.Right < field.Right, $"field {field} label {label} number {number}");
    }
}
