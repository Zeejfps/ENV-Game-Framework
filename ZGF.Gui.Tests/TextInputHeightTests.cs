using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// A field's height must not depend on whether it holds text: a placeholder-only field and the same
/// field with one character in it measure the same, so a box drawn around it does not jump on the
/// first keystroke.
/// </summary>
public class TextInputHeightTests
{
    private const float FieldHeight = 22f;
    private const float Width = 200f;

    private static GuiTestHarness Field(State<string> value, float? height, out TextInputView view)
    {
        // In a column, so the harness sizes the column to the window and the field keeps its own height.
        var h = GuiTestHarness.Create(ctx => new Column
        {
            Children =
            [
                new TextInput
                {
                    Id = "field",
                    Value = value,
                    AutoFocus = true,
                    Height = height is { } fixedHeight ? fixedHeight : default,
                },
            ],
        }.BuildView(ctx));
        view = (TextInputView)h.Get("field");
        return h;
    }

    [Fact]
    public void AnExplicitHeight_HoldsWhileTheFieldIsEmpty()
    {
        var value = new State<string>(string.Empty);
        using var h = Field(value, FieldHeight, out var view);

        Assert.Equal(FieldHeight, view.MeasureHeight(Width));
    }

    [Fact]
    public void TheFirstCharacter_DoesNotChangeTheHeight()
    {
        var value = new State<string>(string.Empty);
        using var h = Field(value, FieldHeight, out var view);
        var empty = view.MeasureHeight(Width);

        h.Type("a");

        Assert.Equal(empty, view.MeasureHeight(Width));
    }

    [Fact]
    public void WithoutAnExplicitHeight_AnEmptyFieldIsOneLineTall()
    {
        var value = new State<string>(string.Empty);
        using var h = Field(value, null, out var view);
        var empty = view.MeasureHeight(Width);

        h.Type("a");

        Assert.Equal(empty, view.MeasureHeight(Width));
    }
}
