using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Components.Controls;

/// <summary>
/// Single-line text input. <see cref="Value"/> is two-way bound: the state drives the input,
/// and the user's edits are pushed back into the state. Keyboard handling, focus and
/// clipboard are wired from the build context.
/// </summary>
public sealed record TextInput : Widget
{
    public required State<string> Value { get; init; }
    public string? Placeholder { get; init; }
    public StyleValue<uint> PlaceholderColor { get; init; }
    public uint Background { get; init; } = 0xFF2A2A2A;
    public StyleValue<uint> Color { get; init; }
    public StyleValue<uint> CaretColor { get; init; }
    public StyleValue<uint> SelectionColor { get; init; }
    public StyleValue<float> FontSize { get; init; }
    public StyleValue<TextAlignment> VAlign { get; init; }

    protected override View CreateView(Context ctx)
    {
        var input = ctx.Require<InputSystem>();
        var clipboard = ctx.Get<IClipboard>();

        var view = new TextInputView(ctx.Canvas)
        {
            BackgroundColor = Background,
            PlaceholderText = Placeholder,
        };
        if (PlaceholderColor.IsSet) view.PlaceholderTextColor = PlaceholderColor;
        if (Color.IsSet) view.TextColor = Color;
        if (CaretColor.IsSet) view.CaretColor = CaretColor.Value;
        if (SelectionColor.IsSet) view.SelectionRectColor = SelectionColor.Value;
        if (FontSize.IsSet) view.FontSize = FontSize;
        if (VAlign.IsSet) view.TextVerticalAlignment = VAlign;

        view.BindTwoWay(Value);
        view.UseController(input, () => new TextInputViewKbmController(view, input, clipboard));
        return view;
    }
}
