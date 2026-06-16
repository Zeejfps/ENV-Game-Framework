using System.Globalization;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarViewModel
{
    public State<DateOnly?> SelectedDate { get; } = new(null);
    public State<DateOnly> DisplayedMonth { get; }
    public State<DateOnly?> MinDate { get; } = new(null);
    public State<DateOnly?> MaxDate { get; } = new(null);
    public State<CultureInfo> Culture { get; } = new(CultureInfo.CurrentCulture);

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Set by the building component: reports whether the calendar's year field holds
    /// keyboard focus. Hosts that float the calendar over their own focused editor use
    /// <see cref="HasKeyboardFocus"/> to avoid blurring/closing the picker when the user clicks
    /// into the year field.</summary>
    public Func<bool>? FocusProbe { get; set; }

    public bool HasKeyboardFocus => FocusProbe?.Invoke() ?? false;

    public CalendarViewModel()
    {
        DisplayedMonth = new State<DateOnly>(new DateOnly(Today.Year, Today.Month, 1));
    }

    public void StepMonth(int delta)
    {
        var month = DisplayedMonth.Value;
        DisplayedMonth.Value = new DateOnly(month.Year, month.Month, 1).AddMonths(delta);
    }

    public void StepYear(int delta) => SetYear(DisplayedMonth.Value.Year + delta);

    public void SetYear(int year)
    {
        var month = DisplayedMonth.Value;
        DisplayedMonth.Value = new DateOnly(ClampYear(year), month.Month, 1);
    }

    public void PickDate(DateOnly date)
    {
        if (IsDisabled(date)) return;
        SelectedDate.Value = date;
        var month = DisplayedMonth.Value;
        if (date.Year != month.Year || date.Month != month.Month)
            DisplayedMonth.Value = new DateOnly(date.Year, date.Month, 1);
    }

    public bool IsDisabled(DateOnly date) =>
        (MinDate.Value.HasValue && date < MinDate.Value.Value)
        || (MaxDate.Value.HasValue && date > MaxDate.Value.Value);

    private int ClampYear(int year)
    {
        if (MinDate.Value.HasValue && year < MinDate.Value.Value.Year) year = MinDate.Value.Value.Year;
        if (MaxDate.Value.HasValue && year > MaxDate.Value.Value.Year) year = MaxDate.Value.Value.Year;
        return Math.Clamp(year, 1, 9999);
    }
}
