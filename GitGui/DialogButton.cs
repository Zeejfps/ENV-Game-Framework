using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class DialogButton : HoverableButton
{
    private readonly TextView _iconView;
    private readonly TextView _labelView;
    private readonly FlexRowView _row;

    public string Label
    {
        get => _labelView.Text ?? string.Empty;
        set => _labelView.Text = value;
    }

    /// <summary>
    /// Icon shown to the left of the label. Empty string detaches it from the row so the
    /// button collapses to just the label — keeping the icon as a hidden child would still
    /// add the row's gap and offset the label centering.
    /// </summary>
    public string Icon
    {
        get => _iconView.Text ?? string.Empty;
        set
        {
            var hasIcon = !string.IsNullOrEmpty(value);
            _iconView.Text = value;
            var attached = _row.Children.Contains(_iconView);
            if (hasIcon && !attached) _row.Children.Insert(0, _iconView);
            else if (!hasIcon && attached) _row.Children.Remove(_iconView);
        }
    }

    public float IconRotation
    {
        get => _iconView.Rotation.Value;
        set => _iconView.Rotation = value;
    }

    public DialogButton(string label, Action? onClick = null) : base(onClick)
    {
        _iconView = new TextView
        {
            Text = string.Empty,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        _iconView.BindThemedTextColor(s => IsEnabled.Value ? s.BorderedButton.Text : s.BorderedButton.TextDisabled);

        _labelView = new TextView
        {
            Text = label,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _labelView.BindThemedTextColor(s => IsEnabled.Value ? s.BorderedButton.Text : s.BorderedButton.TextDisabled);

        _row = new FlexRowView
        {
            Gap = 6,
            MainAxisAlignment = MainAxisAlignment.Center,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { _labelView },
        };

        var background = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(6),
            Children = { _row },
        };
        // Hover styling only when enabled — a disabled button shouldn't react to the pointer.
        // Focus-ring highlighting reuses the same chrome so a tabbed-to button looks hovered.
        BorderedButtonChrome.Bind(background,
            () => IsEnabled.Value && (IsHovered.Value || IsFocusHighlighted.Value));
        SetBackground(background);
    }
}
