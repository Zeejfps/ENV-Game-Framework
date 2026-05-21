using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionButton : HoverableButton
{
    private readonly TextView _iconView;
    private readonly TextView? _labelView;
    private readonly TextView? _badgeText;
    private readonly RowView _countIconGroup;
    private readonly RowView _row;
    private readonly State<bool> _hasBadge = new(false);
    private readonly uint? _badgeColor;
    private readonly uint? _iconColor;

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

    public int? Badge
    {
        set
        {
            if (_badgeText == null) return;
            var attached = _countIconGroup.Children.Contains(_badgeText);
            var visible = value is int n && n > 0;
            if (visible)
            {
                _badgeText.Text = value!.Value.ToString();
                if (!attached) _countIconGroup.Children.Add(_badgeText);
            }
            else if (attached)
            {
                _countIconGroup.Children.Remove(_badgeText);
            }
            _hasBadge.Value = visible;
        }
    }

    public ActionButton(string icon, string? label, Action onClick, string? tooltip = null, uint? badgeColor = null, uint? iconColor = null)
        : base(onClick, tooltip)
    {
        _iconColor = iconColor;
        PreferredHeight = 28;

        _iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _iconView.BindTextColor(ComputeForeground);

        _countIconGroup = new RowView { Gap = 0 };
        _countIconGroup.Children.Add(_iconView);

        _row = new RowView { Gap = 6 };
        _row.Children.Add(_countIconGroup);

        if (!string.IsNullOrEmpty(label))
        {
            _labelView = new TextView
            {
                Text = label,
                VerticalTextAlignment = TextAlignment.Center,
            };
            _labelView.BindTextColor(ComputeLabelForeground);
            _row.Children.Add(_labelView);
        }

        if (badgeColor is uint color)
        {
            _badgeColor = color;
            _badgeText = new TextView
            {
                Text = string.Empty,
                TextColor = color,
                VerticalTextAlignment = TextAlignment.Center,
            };
        }

        var horizontalPadding = _labelView != null ? 8 : 6;
        var background = new RectView
        {
            Children =
            {
                new PaddingView
                {
                    Padding = new PaddingStyle { Left = horizontalPadding, Right = horizontalPadding },
                    Children = { _row },
                }
            }
        };
        background.BindBackgroundColor(() =>
            IsEnabled && IsHovered ? DialogPalette.ButtonHover : 0x00000000u);
        SetBackground(background);
    }

    public ActionButton(string icon, Action onClick, string? tooltip = null, uint? iconColor = null)
        : this(icon, null, onClick, tooltip, iconColor: iconColor) { }

    private uint ComputeForeground()
    {
        if (!IsEnabled) return DialogPalette.RowTextMissing;
        if (_hasBadge && _badgeColor is uint c) return c;
        if (_iconColor is uint ic) return ic;
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
    }

    private uint ComputeLabelForeground()
    {
        if (!IsEnabled) return DialogPalette.RowTextMissing;
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
    }
}
