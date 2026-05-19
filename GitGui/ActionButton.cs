using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class ActionButton : HoverableButton
{
    private readonly TextView _iconView;
    private readonly TextView? _labelView;

    public string Icon
    {
        get => _iconView.Text ?? string.Empty;
        set => _iconView.Text = value;
    }

    public string Label
    {
        get => _labelView?.Text ?? string.Empty;
        set { if (_labelView != null) _labelView.Text = value; }
    }

    public float IconRotation
    {
        get => _iconView.Rotation.Value;
        set => _iconView.Rotation = value;
    }

    public ActionButton(string icon, string? label, Action onClick, string? tooltip = null) : base(onClick)
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

        var row = new RowView { Gap = 6 };
        row.Children.Add(_iconView);

        if (!string.IsNullOrEmpty(label))
        {
            _labelView = new TextView
            {
                Text = label,
                VerticalTextAlignment = TextAlignment.Center,
            };
            _labelView.BindTextColor(ComputeForeground);
            row.Children.Add(_labelView);
        }

        var horizontalPadding = _labelView != null ? 8 : 6;
        var background = new RectView
        {
            Children =
            {
                new PaddingView
                {
                    Padding = new PaddingStyle { Left = horizontalPadding, Right = horizontalPadding },
                    Children = { row },
                }
            }
        };
        background.BindBackgroundColor(() =>
            IsEnabled && IsHovered ? DialogPalette.ButtonHover : 0x00000000u);
        SetBackground(background);

        if (!string.IsNullOrEmpty(tooltip))
        {
            this.UsePresenter(ctx => new Tooltip(this, ctx, tooltip, IsHovered, IsEnabled));
        }
    }

    public ActionButton(string icon, Action onClick, string? tooltip = null)
        : this(icon, null, onClick, tooltip) { }

    private uint ComputeForeground()
    {
        if (!IsEnabled) return DialogPalette.RowTextMissing;
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
    }
}
