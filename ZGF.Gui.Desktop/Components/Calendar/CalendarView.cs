using System.Globalization;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarView : RectView
{
    private const int Columns = 7;
    private const int Rows = 6;
    private const int CellCount = Columns * Rows;
    private const float CellWidth = 36f;
    private const float CellHeight = 32f;

    public State<DateOnly?> SelectedDate { get; } = new(null);
    public State<DateOnly> DisplayedMonth { get; }

    private DateOnly? _minDate;
    public DateOnly? MinDate
    {
        get => _minDate;
        set { _minDate = value; RefreshAll(); }
    }

    private DateOnly? _maxDate;
    public DateOnly? MaxDate
    {
        get => _maxDate;
        set { _maxDate = value; RefreshAll(); }
    }

    private CultureInfo _culture = CultureInfo.CurrentCulture;
    public CultureInfo Culture
    {
        get => _culture;
        set { _culture = value; RefreshAll(); }
    }

    public uint HeaderTextColor { get; set; } = 0xFFE0E0E0;
    public uint WeekdayTextColor { get; set; } = 0xFF9CA3AF;
    public uint NavArrowColor { get; set; } = 0xFFE0E0E0;

    private readonly TextView _title;
    private readonly TextView[] _weekdayLabels = new TextView[Columns];
    private readonly CalendarDayCell[] _cells = new CalendarDayCell[CellCount];
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);

    public CalendarView()
    {
        DisplayedMonth = new State<DateOnly>(new DateOnly(_today.Year, _today.Month, 1));

        BackgroundColor = 0xFF1E1E1E;
        BorderRadius = BorderRadiusStyle.All(6);
        Padding = PaddingStyle.All(10);

        var prev = NavButton("‹", () => StepMonth(-1));
        var next = NavButton("›", () => StepMonth(1));

        _title = new TextView
        {
            FontSize = 15,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = HeaderTextColor,
        };

        var header = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                prev,
                new FlexItem { Grow = 1, Child = _title },
                next,
            }
        };

        var weekdayRow = new RowView();
        for (var i = 0; i < Columns; i++)
        {
            _weekdayLabels[i] = new TextView
            {
                Width = CellWidth,
                Height = 24,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = WeekdayTextColor,
            };
            weekdayRow.Children.Add(_weekdayLabels[i]);
        }

        var grid = new ColumnView { Gap = 2 };
        var cellIndex = 0;
        for (var r = 0; r < Rows; r++)
        {
            var row = new RowView();
            for (var c = 0; c < Columns; c++)
            {
                var cell = new CalendarDayCell
                {
                    Width = CellWidth,
                    Height = CellHeight,
                    Clicked = OnDayClicked,
                };
                cell.UseController(_ => new CalendarDayCellController(cell));
                _cells[cellIndex++] = cell;
                row.Children.Add(cell);
            }
            grid.Children.Add(row);
        }

        Children.Add(new ColumnView
        {
            Gap = 6,
            Children =
            {
                header,
                weekdayRow,
                grid,
            }
        });

        this.Bind(DisplayedMonth, _ => RefreshAll());
        this.Bind(SelectedDate, _ => RefreshAll());

        RefreshAll();
    }

    private TextView NavButton(string glyph, Action onClick)
    {
        var button = new TextView
        {
            Text = glyph,
            Width = 24,
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = NavArrowColor,
        };
        button.UseController(_ => new CalendarNavButtonController(onClick));
        return button;
    }

    private void StepMonth(int delta)
    {
        var month = DisplayedMonth.Value;
        DisplayedMonth.Value = new DateOnly(month.Year, month.Month, 1).AddMonths(delta);
    }

    private void OnDayClicked(DateOnly date)
    {
        SelectedDate.Value = date;
        var month = DisplayedMonth.Value;
        if (date.Year != month.Year || date.Month != month.Month)
            DisplayedMonth.Value = new DateOnly(date.Year, date.Month, 1);
    }

    private void RefreshAll()
    {
        var month = DisplayedMonth.Value;
        var firstOfMonth = new DateOnly(month.Year, month.Month, 1);

        _title.Text = firstOfMonth.ToString("MMMM yyyy", _culture);

        var weekStart = (int)_culture.DateTimeFormat.FirstDayOfWeek;
        var abbreviated = _culture.DateTimeFormat.AbbreviatedDayNames;
        for (var i = 0; i < Columns; i++)
            _weekdayLabels[i].Text = abbreviated[(weekStart + i) % 7];

        var offset = ((int)firstOfMonth.DayOfWeek - weekStart + 7) % 7;
        var gridStart = firstOfMonth.AddDays(-offset);
        var selected = SelectedDate.Value;

        for (var i = 0; i < CellCount; i++)
        {
            var date = gridStart.AddDays(i);
            var inMonth = date.Year == month.Year && date.Month == month.Month;
            var isSelected = selected.HasValue && selected.Value == date;
            var isToday = date == _today;
            var disabled = (_minDate.HasValue && date < _minDate.Value)
                           || (_maxDate.HasValue && date > _maxDate.Value);
            _cells[i].Configure(date, inMonth, isSelected, isToday, disabled);
        }
    }
}
