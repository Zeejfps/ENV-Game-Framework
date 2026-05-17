using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class ActionButton : HoverableButton
{
    private readonly TextView _iconView;
    private readonly TextView _labelView;

    public string Icon
    {
        get => _iconView.Text ?? string.Empty;
        set => _iconView.Text = value;
    }

    public string Label
    {
        get => _labelView.Text ?? string.Empty;
        set => _labelView.Text = value;
    }

    public float IconRotation
    {
        get => _iconView.Rotation.Value;
        set => _iconView.Rotation = value;
    }

    public ActionButton(string icon, string label, Action onClick) : base(onClick)
    {
        PreferredHeight = 28;

        _iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _iconView.BindTextColor(ComputeForeground);

        _labelView = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _labelView.BindTextColor(ComputeForeground);

        var background = new RectView
        {
            Padding = new PaddingStyle { Left = 8, Right = 8 },
            Children =
            {
                new RowView
                {
                    Gap = 6,
                    Children = { _iconView, _labelView },
                }
            }
        };
        background.BindBackgroundColor(() =>
            IsEnabled.Value && IsHovered.Value ? DialogPalette.ButtonHover : 0x00000000u);
        SetBackground(background);
    }

    private uint ComputeForeground()
    {
        if (!IsEnabled.Value) return DialogPalette.RowTextMissing;
        return IsHovered.Value ? 0xFFFFFFFFu : DialogPalette.RowText;
    }
}
