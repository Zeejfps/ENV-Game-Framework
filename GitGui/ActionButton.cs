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
    // Tight inner row holding [icon, count?]. Nested so the count hugs the icon while
    // the outer _row keeps a normal gap to the label — without the nesting, every gap in
    // the row would be the same width and the count would visually float between two
    // evenly-spaced columns instead of belonging to the icon.
    private readonly RowView _countIconGroup;
    private readonly RowView _row;
    // When the badge is visible, the icon adopts the badge colour so the arrow + number
    // read as one coloured unit (matching the sidebar's branch-row badges). State so
    // BindTextColor can auto-track it and re-run ComputeForeground on flip.
    private readonly State<bool> _hasBadge = new(false);
    private readonly uint? _badgeColor;

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

    // Null or 0 hides; non-zero appends the count right after the icon inside the tight
    // count/icon group. The badge keeps its colour even when the button is disabled —
    // the count is informational and stays readable regardless of whether the action is
    // currently available.
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

    public ActionButton(string icon, string? label, Action onClick, string? tooltip = null, uint? badgeColor = null)
        : base(onClick, tooltip)
    {
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
            _labelView.BindTextColor(ComputeForeground);
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
            // Not attached to _row yet — the Badge setter inserts/removes it as needed.
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

    public ActionButton(string icon, Action onClick, string? tooltip = null)
        : this(icon, null, onClick, tooltip) { }

    private uint ComputeForeground()
    {
        if (!IsEnabled) return DialogPalette.RowTextMissing;
        // When the badge is showing, the icon adopts the badge colour so the arrow + count
        // read as one unit — and stays that colour even on hover, since the count text
        // doesn't flash on hover and we don't want the pair to split visually.
        if (_hasBadge && _badgeColor is uint c) return c;
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
    }
}
