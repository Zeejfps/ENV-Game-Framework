using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class ActionButton : HoverableButton
{
    private readonly TextView _iconView;
    private readonly TextView? _labelView;
    private readonly TextView? _badgeView;
    private readonly RowView _row;

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

    // Optional small count chip rendered after the label (e.g. "3" on Push). Set to null
    // or 0 to hide; non-zero pops it back in. Colour is fixed at construction time so the
    // call-site doesn't have to re-pass it on every update. The badge keeps its colour
    // even when the button is disabled — the count is informational and stays readable
    // regardless of whether the action is currently available (a disabled Push still
    // wants to show how many commits are ahead, just dimmed by the count colour itself).
    public int? Badge
    {
        set
        {
            if (_badgeView == null) return;
            var attached = _row.Children.Contains(_badgeView);
            if (value is int n && n > 0)
            {
                _badgeView.Text = n.ToString();
                if (!attached) _row.Children.Add(_badgeView);
            }
            else if (attached)
            {
                _row.Children.Remove(_badgeView);
            }
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
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _iconView.BindTextColor(ComputeForeground);

        _row = new RowView { Gap = 6 };
        _row.Children.Add(_iconView);

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
            _badgeView = new TextView
            {
                Text = string.Empty,
                TextColor = color,
                VerticalTextAlignment = TextAlignment.Center,
            };
            // Not added to _row yet — Badge setter inserts it on first non-zero value.
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
        return IsHovered ? 0xFFFFFFFFu : DialogPalette.RowText;
    }
}
