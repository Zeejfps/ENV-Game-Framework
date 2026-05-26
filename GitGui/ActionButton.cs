using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionButton : HoverableButton
{
    private readonly uint? _badgeColor;
    private readonly uint? _iconColor;
    private readonly uint? _backgroundColor;

    // Theme-reactive holders read by ComputeBackground / ComputeForeground / ComputeLabelForeground
    // via the Derived auto-tracking path.
    private readonly State<uint> _buttonHover = new(ThemePresets.Dark.Dialog.ButtonHover);
    private readonly State<uint> _rowText = new(ThemePresets.Dark.Dialog.RowText);
    private readonly State<uint> _rowTextMissing = new(ThemePresets.Dark.Dialog.RowTextMissing);
    private readonly State<uint> _textStrong = new(ThemePresets.Dark.Text.Strong);

    public State<string> Icon { get; }
    public State<string> Label { get; }
    public State<float> IconRotation { get; } = new(0f);
    public State<int?> Badge { get; } = new(null);

    public ActionButton(string icon, string? label = null, string? tooltip = null, uint? badgeColor = null, uint? iconColor = null, uint? backgroundColor = null)
        : base(null, tooltip)
    {
        Icon = new State<string>(icon);
        Label = new State<string>(label ?? string.Empty);

        _backgroundColor = backgroundColor;
        _iconColor = iconColor ?? (backgroundColor != null ? 0xFFFFFFFFu : (uint?)null);
        PreferredHeight = 28;

        var iconView = new TextView
        {
            FontFamily = LucideIcons.FontFamily,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindText(Icon);
        iconView.BindTextColor(ComputeForeground);
        iconView.BindRotation(IconRotation);

        var countIconGroup = new RowView { Gap = 0, Children = { iconView } };

        if (badgeColor is uint color)
        {
            _badgeColor = color;
            var badgeText = new TextView
            {
                TextColor = color,
                VerticalTextAlignment = TextAlignment.Center,
            };
            badgeText.BindText(Badge, n => n?.ToString() ?? string.Empty);
            badgeText.BindIsVisible(Badge, n => n is > 0);
            countIconGroup.Children.Add(badgeText);
        }

        var row = new RowView { Gap = 6, Children = { countIconGroup } };

        TextView? labelView = null;
        if (!string.IsNullOrEmpty(label))
        {
            labelView = new TextView
            {
                VerticalTextAlignment = TextAlignment.Center,
            };
            labelView.BindText(Label);
            labelView.BindTextColor(ComputeLabelForeground);
            row.Children.Add(labelView);
        }

        var horizontalPadding = labelView != null ? 8 : (_backgroundColor != null ? 10 : 6);
        var background = new RectView
        {
            BorderRadius = _backgroundColor != null ? BorderRadiusStyle.All(6) : default,
            Children =
            {
                new PaddingView
                {
                    Padding = new PaddingStyle { Left = horizontalPadding, Right = horizontalPadding },
                    Children = { row },
                }
            }
        };
        background.BindBackgroundColor(ComputeBackground);
        SetBackground(background);

        this.BindToTheme(t =>
        {
            _buttonHover.Value = t.Dialog.ButtonHover;
            _rowText.Value = t.Dialog.RowText;
            _rowTextMissing.Value = t.Dialog.RowTextMissing;
            _textStrong.Value = t.Text.Strong;
        });
    }

    private uint ComputeBackground()
    {
        if (_backgroundColor is uint bg)
        {
            if (!IsEnabled) return Darken(bg, 0x40);
            return IsHovered ? Lighten(bg, 0x18) : bg;
        }
        return IsEnabled && IsHovered ? _buttonHover.Value : 0x00000000u;
    }

    private uint ComputeForeground()
    {
        if (!IsEnabled) return _rowTextMissing.Value;
        if (Badge.Value is > 0 && _badgeColor is uint c) return c;
        if (_iconColor is uint ic) return ic;
        return IsHovered ? _textStrong.Value : _rowText.Value;
    }

    private uint ComputeLabelForeground()
    {
        if (!IsEnabled) return _rowTextMissing.Value;
        return IsHovered ? _textStrong.Value : _rowText.Value;
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
}
