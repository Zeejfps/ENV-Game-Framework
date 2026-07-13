using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Components.Controls;

/// <summary>
/// Text input. <see cref="Value"/> is a two-way <see cref="Prop{T}"/>: its source
/// drives the input and the user's edits are written back through it. Keyboard handling, focus
/// and clipboard are wired from the build context. Single-line by default; set <see cref="Wrap"/>
/// to <see cref="TextWrap.Wrap"/> for a multi-line editor whose intrinsic height grows with its
/// wrapped content.
/// </summary>
public sealed record TextInput : Widget
{
    public required Prop<string> Value { get; init; }
    public Prop<string?> Placeholder { get; init; }
    public Prop<TextWrap> Wrap { get; init; }
    public Prop<uint> PlaceholderColor { get; init; }
    public Prop<uint> Background { get; init; } = 0xFF2A2A2A;
    public Prop<uint> Color { get; init; }
    public Prop<uint> CaretColor { get; init; }
    public Prop<uint> SelectionColor { get; init; }
    public Prop<float> FontSize { get; init; }
    public Prop<TextAlignment> VAlign { get; init; }

    /// <summary>When true the field grabs keyboard focus as soon as it mounts, so a field revealed in
    /// response to a click (a search bar, a just-opened editor) is ready to type without a second click.
    /// Mirrors Flutter's <c>autofocus</c>. A plain init flag rather than a reactive <see cref="Prop{T}"/>
    /// because it's a one-time mount decision, like <see cref="Widget.Id"/>.</summary>
    public bool AutoFocus { get; init; }

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

        // Created eagerly (not via the lazy factory) so AutoFocus can hold the controller and drive it.
        var controller = new TextInputViewKbmController(view, input, clipboard)
        {
            // Nearest enclosing scroll container, if any — keeps the caret in view as it moves.
            ScrollScope = ctx.Get<IScrollScope>(),
        };
        // Wrapping is what makes the field multi-line, which is also what decides whether Enter
        // breaks the line or bubbles to the owner as a submit.
        Wrap.Apply(ctx, view, (v, w) =>
        {
            v.TextWrap = w;
            controller.IsMultiLine = w == TextWrap.Wrap;
        });
        view.UseController(input, controller);
        if (AutoFocus)
            view.Behaviors.Add(new FocusOnMount(controller));
        return view;
    }

    // Grabs the caret on mount and releases it on unmount, so the field is ready to type the moment it
    // appears and doesn't leave a detached caret stealing keys once it's gone.
    private sealed class FocusOnMount : IViewBehavior
    {
        private readonly TextInputViewKbmController _controller;
        public FocusOnMount(TextInputViewKbmController controller) => _controller = controller;
        public void Attach(View view) => _controller.BeginEditing();
        public void Detach(View view) => _controller.EndEditing();
    }
}
