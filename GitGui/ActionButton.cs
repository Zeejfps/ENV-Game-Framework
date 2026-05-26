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

    public State<string> Icon { get; }
    public State<string> Label { get; }
    public State<float> IconRotation { get; } = new(0f);
    public State<int?> Badge { get; } = new(null);
    public State<Command?> Command { get; } = new(null);

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
    }

    /// <summary>
    /// Wires <paramref name="command"/> as the click handler and binds <see cref="HoverableButton.IsEnabled"/>
    /// to its <see cref="ZGF.Observable.Command.CanExecute"/>. Call from a VM bind callback —
    /// when the VM is disposed, the source <c>CanExecute</c>'s subscribers are cleared and the
    /// wiring goes dormant.
    /// </summary>
    public void BindCommand(Command command)
    {
        Command.Value = command;
        IsEnabled.BindTo(command.CanExecute);
    }

    protected override void OnClicked() => Command.Value?.Execute();

    private uint ComputeBackground()
    {
        if (_backgroundColor is uint bg)
        {
            if (!IsEnabled) return Darken(bg, 0x40);
            return IsHovered ? Lighten(bg, 0x18) : bg;
        }
        return IsEnabled && IsHovered ? DialogPalette.ButtonHover : 0x00000000u;
    }

    private uint ComputeForeground()
    {
        if (!IsEnabled) return DialogPalette.RowTextMissing;
        if (Badge.Value is > 0 && _badgeColor is uint c) return c;
        if (_iconColor is uint ic) return ic;
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
    }

    private uint ComputeLabelForeground()
    {
        if (!IsEnabled) return DialogPalette.RowTextMissing;
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
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
