using System.Globalization;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
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

    /// <summary>True while the calendar's year field holds keyboard focus. Hosts that float the
    /// calendar over their own focused editor use this to avoid blurring/closing the picker when the
    /// user clicks into the year field.</summary>
    public bool HasKeyboardFocus
    {
        get
        {
            var input = Context?.Get<InputSystem>();
            return input is not null && ReferenceEquals(input.FocusedComponent, _yearController);
        }
    }

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
    public uint NavButtonColor { get; set; } = 0x00000000;
    public uint NavButtonHoverColor { get; set; } = 0xFF333333;
    public uint YearFieldColor { get; set; } = 0xFF2A2A2A;

    private readonly TextView _monthLabel;
    private readonly TextInputView _yearInput;
    private readonly CalendarYearInputController _yearController;
    private readonly TextView[] _weekdayLabels = new TextView[Columns];
    private readonly CalendarDayCell[] _cells = new CalendarDayCell[CellCount];
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);

    public CalendarView()
    {
        DisplayedMonth = new State<DateOnly>(new DateOnly(_today.Year, _today.Month, 1));

        BackgroundColor = 0xFF1E1E1E;
        BorderRadius = BorderRadiusStyle.All(6);
        Padding = PaddingStyle.All(10);

        var prev = NavButton("‹", 18, new PaddingStyle { Left = 8, Right = 8, Top = 3, Bottom = 3 }, () => StepMonth(-1));
        var next = NavButton("›", 18, new PaddingStyle { Left = 8, Right = 8, Top = 3, Bottom = 3 }, () => StepMonth(1));

        _monthLabel = new TextView
        {
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = HeaderTextColor,
        };

        _yearInput = new TextInputView
        {
            Width = 40,
            Height = 20,
            FontSize = 15,
            BackgroundColor = 0x00000000,
            TextColor = HeaderTextColor,
            CaretColor = HeaderTextColor,
            SelectionRectColor = 0xFF3B82F6,
            TextVerticalAlignment = TextAlignment.Center,
        };
        _yearController = new CalendarYearInputController(_yearInput, CommitYear, RevertYear);
        _yearInput.UseController(_ => _yearController);

        var yearField = new RectView
        {
            BackgroundColor = YearFieldColor,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 2, Bottom = 2 },
            Children = { _yearInput },
        };

        var spinner = new ColumnView
        {
            Children =
            {
                NavButton("▲", 8, new PaddingStyle { Left = 4, Right = 4, Top = 1, Bottom = 1 }, () => StepYear(1)),
                NavButton("▼", 8, new PaddingStyle { Left = 4, Right = 4, Top = 1, Bottom = 1 }, () => StepYear(-1)),
            }
        };

        var titleCluster = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.Center,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Gap = 6,
            Children =
            {
                _monthLabel,
                yearField,
                spinner,
            }
        };

        var header = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                prev,
                new FlexItem { Grow = 1, Child = titleCluster },
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

    private RectView NavButton(string glyph, float fontSize, PaddingStyle padding, Action onClick)
    {
        var label = new TextView
        {
            Text = glyph,
            FontSize = fontSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = NavArrowColor,
        };
        var button = new RectView
        {
            BackgroundColor = NavButtonColor,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = padding,
            Children = { label },
        };
        button.UseController(_ => new CalendarNavButtonController(button, onClick, NavButtonColor, NavButtonHoverColor));
        return button;
    }

    private void StepMonth(int delta)
    {
        var month = DisplayedMonth.Value;
        DisplayedMonth.Value = new DateOnly(month.Year, month.Month, 1).AddMonths(delta);
    }

    private void StepYear(int delta) => SetYear(DisplayedMonth.Value.Year + delta);

    private void SetYear(int year)
    {
        var month = DisplayedMonth.Value;
        DisplayedMonth.Value = new DateOnly(ClampYear(year), month.Month, 1);
    }

    private int ClampYear(int year)
    {
        if (_minDate.HasValue && year < _minDate.Value.Year) year = _minDate.Value.Year;
        if (_maxDate.HasValue && year > _maxDate.Value.Year) year = _maxDate.Value.Year;
        return Math.Clamp(year, 1, 9999);
    }

    private void CommitYear(string text)
    {
        if (int.TryParse(text.Trim(), out var year))
            SetYear(year);
        RefreshAll();
    }

    private void RevertYear() => RefreshAll();

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

        _monthLabel.Text = firstOfMonth.ToString("MMMM", _culture);
        if (!_yearInput.IsEditing)
            _yearInput.SetText(month.Year.ToString(CultureInfo.InvariantCulture));

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
