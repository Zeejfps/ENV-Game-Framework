using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class NumberFieldTests
{
    private const float Width = 120f;

    private int _value = 5;
    private bool _changed;
    private readonly UiFrames _frames;

    public NumberFieldTests() : this(UiTheme.Dark)
    {
    }

    private NumberFieldTests(UiTheme theme)
    {
        _frames = new UiFrames(new DefaultUiState { Theme = theme }, body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.Row(new Box { Gap = 8f }))
            {
                _changed |= ui.NumberField("number", ref _value, 1, 10, Width);
                ui.Button("elsewhere");
            }
        });
        _frames.Frame(2);
    }

    private UiRect Field => _frames.RectOf("number");

    private float Middle => Field.Y + Field.Height / 2f;

    private void ClickValue() => _frames.Click(Field.X + Field.Width / 2f, Middle).Frame(2);

    [Fact]
    public void Its_arrows_step_the_value_and_stop_at_its_ends()
    {
        var increase = Field.Right - 4f;
        _frames.Click(increase, Middle).Frame();
        Assert.Equal(6, _value);
        Assert.True(_changed);

        for (var i = 0; i < 8; i++)
            _frames.Click(increase, Middle).Frame();
        Assert.Equal(10, _value);

        _frames.Click(Field.X + 4f, Middle).Frame();
        Assert.Equal(9, _value);
    }

    [Fact]
    public void Dragging_the_value_sideways_scrubs_it_without_typing()
    {
        var start = Field.X + Field.Width / 2f;
        _frames.MovePointer(start, Middle).Press().Frame();
        _frames.MovePointer(start + 18f, Middle).Frame();
        Assert.Equal(8, _value);

        _frames.Release().Frame();
        _frames.Type("2").Frame();
        Assert.Equal(8, _value);
    }

    [Fact]
    public void A_click_on_the_value_types_over_it_and_Enter_takes_it_kept_in_range()
    {
        ClickValue();
        _frames.Type("42").Frame();
        _frames.Key(KeyboardKey.Enter).Frame();

        Assert.Equal(10, _value);
    }

    [Fact]
    public void The_value_holds_still_as_a_click_starts_typing_over_it()
    {
        var shown = _frames.TextRect("5").Right;

        _frames.Click(Field.X + Field.Width / 2f, Middle);
        for (var frame = 0; frame < 3; frame++)
        {
            _frames.Frame();
            Assert.Equal(shown, _frames.TextRect("5").Right, 1f);
        }
    }

    [Fact]
    public void Escape_drops_what_was_typed()
    {
        ClickValue();
        _frames.Type("3").Frame();
        _frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.Equal(5, _value);
        Assert.False(_changed);
    }

    [Fact]
    public void The_focus_leaving_takes_what_was_typed()
    {
        ClickValue();
        _frames.Type("3").Frame();
        _frames.ClickOn("elsewhere").Frame();

        Assert.Equal(3, _value);
    }

    [Fact]
    public void The_wheel_over_it_steps_it()
    {
        _frames.MovePointer(Field).Frame();
        _frames.Wheel(2).Frame();

        Assert.Equal(7, _value);
    }

    [Fact]
    public void Up_and_Down_step_it_while_it_has_the_focus()
    {
        _frames.Key(KeyboardKey.Tab).Frame(2);
        _frames.Key(KeyboardKey.UpArrow).Frame();
        _frames.Key(KeyboardKey.DownArrow).Frame();
        _frames.Key(KeyboardKey.DownArrow).Frame();

        Assert.Equal(4, _value);
    }

    [Fact]
    public void The_themes_painter_draws_it_and_the_behavior_stays()
    {
        var painted = 0;
        var test = new NumberFieldTests(UiTheme.Dark with
        {
            NumberFieldPainter = (Ui ui, in NumberFieldState state, in NumberFieldFace face) =>
            {
                painted++;
                using (ui.Box(state.Id, new Box { Width = face.Width, Height = 24f }))
                    ui.Text(state.Text);
            },
        });

        test._frames.Click(test.Field.Right - 4f, test.Middle).Frame();

        Assert.True(painted > 0);
        Assert.Equal(6, test._value);
        Assert.True(test._frames.Drew("6"));
    }
}
