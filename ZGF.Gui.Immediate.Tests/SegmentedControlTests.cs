using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SegmentedControlTests
{
    private enum Language
    {
        CSharp,
        IL,
        Both,
    }

    private static readonly Segment<Language>[] Segments =
    [
        new(Language.CSharp, "C#", "Decompile to C#"),
        new(Language.IL, "IL"),
        new(Language.Both, "Both"),
    ];

    private readonly UiFrames _frames;
    private Language _language;
    private int _changes;

    public SegmentedControlTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        if (ui.SegmentedControl("lang", ref _language, Segments))
            _changes++;
    }

    private static Func<Ui, int> Segment(int index) => ui => UiWidgets.SegmentId(ui, ui.GetId("lang"), index);

    [Fact]
    public void The_selected_segment_is_drawn_in_the_selection_roles()
    {
        _frames.Frame();

        Assert.Equal(UiTheme.Dark.SelectionText, _frames.TextColor("C#"));
        Assert.Equal(UiTheme.Dark.TextMuted, _frames.TextColor("IL"));
        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.Selection);
        Assert.DoesNotContain(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.Accent);
    }

    [Fact]
    public void Clicking_a_segment_selects_it_and_reports_the_change_once()
    {
        _frames.Frame();

        _frames.ClickOn(Segment(1)).Frame();

        Assert.Equal(Language.IL, _language);
        Assert.Equal(1, _changes);
    }

    [Fact]
    public void Clicking_the_selected_segment_reports_nothing()
    {
        _frames.Frame();

        _frames.ClickOn(Segment(0));

        Assert.Equal(0, _changes);
    }

    [Fact]
    public void Arrow_keys_step_through_the_segments_once_focused()
    {
        _frames.Frame();
        _frames.ClickOn(Segment(0));

        _frames.Key(KeyboardKey.RightArrow).Frame();
        Assert.Equal(Language.IL, _language);

        _frames.Key(KeyboardKey.End).Frame();
        Assert.Equal(Language.Both, _language);

        _frames.Key(KeyboardKey.RightArrow).Frame();
        Assert.Equal(Language.Both, _language);

        _frames.Key(KeyboardKey.LeftArrow).Frame();
        Assert.Equal(Language.IL, _language);
    }

    [Fact]
    public void Arrow_keys_do_nothing_unfocused()
    {
        _frames.Frame();

        _frames.Key(KeyboardKey.RightArrow).Frame();

        Assert.Equal(Language.CSharp, _language);
    }

    [Fact]
    public void A_segment_shows_its_tooltip_on_hover()
    {
        _frames.Frame();
        var rect = _frames.RectOf(Segment(0));
        _frames.MovePointer(rect.X + 2f, rect.Y + 2f).Frame();
        _frames.Wait(0.6).Frame();

        Assert.True(_frames.Drew("Decompile to C#"));
    }
}
