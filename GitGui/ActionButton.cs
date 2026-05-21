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
    private readonly uint? _backgroundColor;

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

    public ActionButton(string icon, string? label, Action onClick, string? tooltip = null, uint? badgeColor = null, uint? iconColor = null, uint? backgroundColor = null)
        : base(onClick, tooltip)
    {
        _backgroundColor = backgroundColor;
        _iconColor = iconColor ?? (backgroundColor != null ? 0xFFFFFFFFu : (uint?)null);
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

        var horizontalPadding = _labelView != null ? 8 : (_backgroundColor != null ? 10 : 6);
        var background = new RectView
        {
            BorderRadius = _backgroundColor != null ? BorderRadiusStyle.All(6) : default,
            Children =
            {
                new PaddingView
                {
                    Padding = new PaddingStyle { Left = horizontalPadding, Right = horizontalPadding },
                    Children = { _row },
                }
            }
        };
        background.BindBackgroundColor(ComputeBackground);
        SetBackground(background);
    }

    private uint ComputeBackground()
    {
        if (_backgroundColor is uint bg)
        {
            if (!IsEnabled) return Darken(bg, 0x40);
            return IsHovered ? Lighten(bg, 0x18) : bg;
        }
        return IsEnabled && IsHovered ? DialogPalette.ButtonHover : 0x00000000u;
    }

    private static uint Lighten(uint argb, uint delta)
    {
        var a = (argb >> 24) & 0xFF;
        var r = Math.Min(0xFFu, ((argb >> 16) & 0xFF) + delta);
        var g = Math.Min(0xFFu, ((argb >> 8) & 0xFF) + delta);
        var b = Math.Min(0xFFu, (argb & 0xFF) + delta);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    private static uint Darken(uint argb, uint delta)
    {
        var a = (argb >> 24) & 0xFF;
        var r = ((argb >> 16) & 0xFF);
        var g = ((argb >> 8) & 0xFF);
        var b = (argb & 0xFF);
        r = r > delta ? r - delta : 0;
        g = g > delta ? g - delta : 0;
        b = b > delta ? b - delta : 0;
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    public ActionButton(string icon, Action onClick, string? tooltip = null, uint? iconColor = null, uint? backgroundColor = null)
        : this(icon, null, onClick, tooltip, iconColor: iconColor, backgroundColor: backgroundColor) { }

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
