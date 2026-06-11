using ZGF.Gui;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Prototype.Components;

public sealed record Button : Primitive
{
    public required string Label { get; init; }
    public required Action OnClick { get; init; }
    public uint Background { get; init; } = 0xFF3B82F6;
    public uint HoverBackground { get; init; } = 0xFF2563EB;
    public uint TextColor { get; init; } = 0xFFFFFFFF;
    public StyleValue<float> FontSize { get; init; }
    public PaddingStyle Padding { get; init; } = new() { Left = 10, Right = 10, Top = 4, Bottom = 4 };

    protected override View CreateView(Context ctx)
    {
        var label = new TextView
        {
            Text = Label,
            TextColor = TextColor,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        if (FontSize.IsSet) label.FontSize = FontSize;

        var button = new RectView
        {
            BackgroundColor = Background,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = Padding,
            Children = { label },
        };
        button.UseController(_ => new ButtonController(button, OnClick, Background, HoverBackground));
        return button;
    }
}

internal sealed class ButtonController : KeyboardMouseController
{
    private readonly RectView _button;
    private readonly Action _onClick;
    private readonly uint _normalColor;
    private readonly uint _hoverColor;

    public ButtonController(RectView button, Action onClick, uint normalColor, uint hoverColor)
    {
        _button = button;
        _onClick = onClick;
        _normalColor = normalColor;
        _hoverColor = hoverColor;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _button.BackgroundColor = _hoverColor;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _button.BackgroundColor = _normalColor;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed || e.Button != MouseButton.Left) return;

        _onClick();
        e.Consume();
    }
}
