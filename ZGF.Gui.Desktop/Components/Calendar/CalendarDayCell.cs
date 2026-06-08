using ZGF.Gui;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarDayCell : MultiChildView
{
    private readonly RectView _bg;
    private readonly TextView _label;

    private bool _inMonth;
    private bool _selected;
    private bool _today;
    private bool _hovered;

    public DateOnly Date { get; private set; }
    public bool IsDisabled { get; private set; }

    public Action<DateOnly>? Clicked;

    public uint NormalTextColor = 0xFFE0E0E0;
    public uint MutedTextColor = 0xFF6B7280;
    public uint DisabledTextColor = 0xFF4B4B4B;
    public uint SelectedTextColor = 0xFFFFFFFF;
    public uint NormalBackgroundColor = 0x00000000;
    public uint HoverBackgroundColor = 0xFF333333;
    public uint SelectedBackgroundColor = 0xFF3B82F6;
    public uint TodayRingColor = 0xFF3B82F6;

    public CalendarDayCell()
    {
        _label = new TextView
        {
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _bg = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { _label },
        };

        AddChildToSelf(_bg);
    }

    public void Configure(DateOnly date, bool inMonth, bool selected, bool today, bool disabled)
    {
        Date = date;
        _inMonth = inMonth;
        _selected = selected;
        _today = today;
        IsDisabled = disabled;
        _label.Text = date.Day.ToString();
        UpdateVisual();
    }

    public void SetHovered(bool hovered)
    {
        if (_hovered == hovered) return;
        _hovered = hovered;
        UpdateVisual();
    }

    public void RaiseClicked()
    {
        if (IsDisabled) return;
        Clicked?.Invoke(Date);
    }

    private void UpdateVisual()
    {
        uint background;
        uint foreground;

        if (_selected)
        {
            background = SelectedBackgroundColor;
            foreground = SelectedTextColor;
        }
        else if (_hovered && !IsDisabled)
        {
            background = HoverBackgroundColor;
            foreground = _inMonth ? NormalTextColor : MutedTextColor;
        }
        else
        {
            background = NormalBackgroundColor;
            foreground = IsDisabled ? DisabledTextColor : _inMonth ? NormalTextColor : MutedTextColor;
        }

        _bg.BackgroundColor = background;
        _label.TextColor = foreground;

        if (_today && !_selected)
        {
            _bg.BorderColor = BorderColorStyle.All(TodayRingColor);
            _bg.BorderSize = BorderSizeStyle.All(1);
        }
        else
        {
            _bg.BorderSize = BorderSizeStyle.All(0);
        }
    }
}
