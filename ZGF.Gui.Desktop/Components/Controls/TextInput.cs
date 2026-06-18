using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Components.Controls;

/// <summary>
/// Single-line text input. <see cref="Value"/> is a two-way <see cref="Prop{T}"/>: its source
/// drives the input and the user's edits are written back through it. Keyboard handling, focus
/// and clipboard are wired from the build context.
/// </summary>
public sealed record TextInput : Widget
{
    public required Prop<string> Value { get; init; }
    public Prop<string?> Placeholder { get; init; }
    public Prop<uint> PlaceholderColor { get; init; }
    public Prop<uint> Background { get; init; } = 0xFF2A2A2A;
    public Prop<uint> Color { get; init; }
    public Prop<uint> CaretColor { get; init; }
    public Prop<uint> SelectionColor { get; init; }
    public Prop<float> FontSize { get; init; }
    public Prop<TextAlignment> VAlign { get; init; }

    protected override View CreateView(Context ctx)
    {
        var input = ctx.Require<InputSystem>();
        var clipboard = ctx.Get<IClipboard>();

        var view = new TextInputView(ctx.Canvas);
        Background.Apply(ctx, view, static (v, c) => v.BackgroundColor = c);
        Placeholder.Apply(ctx, view, static (v, p) => v.PlaceholderText = p);
        PlaceholderColor.Apply(ctx, view, static (v, c) => v.PlaceholderTextColor = c);
        Color.Apply(ctx, view, static (v, c) => v.TextColor = c);
        CaretColor.Apply(ctx, view, static (v, c) => v.CaretColor = c);
        SelectionColor.Apply(ctx, view, static (v, c) => v.SelectionRectColor = c);
        FontSize.Apply(ctx, view, static (v, c) => v.FontSize = c);
        VAlign.Apply(ctx, view, static (v, a) => v.TextVerticalAlignment = a);

        view.BindTwoWay(Value.ToReadable(ctx), Value.Write);
        view.UseController(input, () => new TextInputViewKbmController(view, input, clipboard));
        return view;
    }
}
