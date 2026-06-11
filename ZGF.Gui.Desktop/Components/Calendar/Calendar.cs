using System.Globalization;
using ZGF.Gui.Bindings;
using ZGF.Gui.Components;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.Calendar;

/// <summary>
/// Month-grid date picker. State and logic live in <see cref="CalendarViewModel"/> (resolved
/// from the build context); this component only assembles views and wires bindings.
/// <see cref="CalendarDayCell"/> stays a View — the grid drives it imperatively
/// (Configure/SetHovered) on every refresh.
/// </summary>
public sealed record Calendar : Widget
{
    private const int Columns = 7;
    private const int Rows = 6;
    private const int CellCount = Columns * Rows;
    private const float CellWidth = 36f;
    private const float CellHeight = 32f;

    public uint BackgroundColor { get; init; } = 0xFF1E1E1E;
    public uint HeaderTextColor { get; init; } = 0xFFE0E0E0;
    public uint WeekdayTextColor { get; init; } = 0xFF9CA3AF;
    public uint NavArrowColor { get; init; } = 0xFFE0E0E0;
    public uint NavButtonColor { get; init; } = 0x00000000;
    public uint NavButtonHoverColor { get; init; } = 0xFF333333;
    public uint YearFieldColor { get; init; } = 0xFF2A2A2A;

    protected override View CreateView(Context ctx)
    {
        var vm = ctx.Require<CalendarViewModel>();
        var canvas = ctx.Canvas;
        var input = ctx.Require<InputSystem>();

        var monthLabel = new TextView(canvas)
        {
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = HeaderTextColor,
        };

        var yearInput = new TextInputView(canvas)
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

        var weekdayLabels = new TextView[Columns];
        var cells = new CalendarDayCell[CellCount];

        void RefreshAll()
        {
            var culture = vm.Culture.Value;
            var month = vm.DisplayedMonth.Value;
            var firstOfMonth = new DateOnly(month.Year, month.Month, 1);

            monthLabel.Text = firstOfMonth.ToString("MMMM", culture);
            if (!yearInput.IsEditing)
                yearInput.SetText(month.Year.ToString(CultureInfo.InvariantCulture));

            var weekStart = (int)culture.DateTimeFormat.FirstDayOfWeek;
            var abbreviated = culture.DateTimeFormat.AbbreviatedDayNames;
            for (var i = 0; i < Columns; i++)
                weekdayLabels[i].Text = abbreviated[(weekStart + i) % 7];

            var offset = ((int)firstOfMonth.DayOfWeek - weekStart + 7) % 7;
            var gridStart = firstOfMonth.AddDays(-offset);
            var selected = vm.SelectedDate.Value;

            for (var i = 0; i < CellCount; i++)
            {
                var date = gridStart.AddDays(i);
                var inMonth = date.Year == month.Year && date.Month == month.Month;
                var isSelected = selected.HasValue && selected.Value == date;
                var isToday = date == vm.Today;
                cells[i].Configure(date, inMonth, isSelected, isToday, vm.IsDisabled(date));
            }
        }

        var yearController = new CalendarYearInputController(
            yearInput, input,
            onCommit: text =>
            {
                if (int.TryParse(text.Trim(), out var year))
                    vm.SetYear(year);
                RefreshAll();
            },
            onRevert: RefreshAll);
        yearInput.UseController(input, yearController);
        vm.FocusProbe = () => ReferenceEquals(input.FocusedComponent, yearController);

        RectView NavButton(string glyph, float fontSize, PaddingStyle padding, Action onClick)
        {
            var label = new TextView(canvas)
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
            button.UseController(input, () => new CalendarNavButtonController(button, onClick, NavButtonColor, NavButtonHoverColor));
            return button;
        }

        var prev = NavButton("‹", 18, new PaddingStyle { Left = 8, Right = 8, Top = 3, Bottom = 3 }, () => vm.StepMonth(-1));
        var next = NavButton("›", 18, new PaddingStyle { Left = 8, Right = 8, Top = 3, Bottom = 3 }, () => vm.StepMonth(1));

        var yearField = new RectView
        {
            BackgroundColor = YearFieldColor,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 2, Bottom = 2 },
            Children = { yearInput },
        };

        var spinner = new ColumnView
        {
            Children =
            {
                NavButton("▲", 8, new PaddingStyle { Left = 4, Right = 4, Top = 1, Bottom = 1 }, () => vm.StepYear(1)),
                NavButton("▼", 8, new PaddingStyle { Left = 4, Right = 4, Top = 1, Bottom = 1 }, () => vm.StepYear(-1)),
            }
        };

        var titleCluster = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.Center,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Gap = 6,
            Children =
            {
                monthLabel,
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
            weekdayLabels[i] = new TextView(canvas)
            {
                Width = CellWidth,
                Height = 24,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = WeekdayTextColor,
            };
            weekdayRow.Children.Add(weekdayLabels[i]);
        }

        var grid = new ColumnView { Gap = 2 };
        var cellIndex = 0;
        for (var r = 0; r < Rows; r++)
        {
            var row = new RowView();
            for (var c = 0; c < Columns; c++)
            {
                var cell = new CalendarDayCell(canvas)
                {
                    Width = CellWidth,
                    Height = CellHeight,
                    Clicked = vm.PickDate,
                };
                cell.UseController(input, () => new CalendarDayCellController(cell));
                cells[cellIndex++] = cell;
                row.Children.Add(cell);
            }
            grid.Children.Add(row);
        }

        var root = new RectView
        {
            BackgroundColor = BackgroundColor,
            BorderRadius = BorderRadiusStyle.All(6),
            Padding = PaddingStyle.All(10),
            Children =
            {
                new ColumnView
                {
                    Gap = 6,
                    Children =
                    {
                        header,
                        weekdayRow,
                        grid,
                    }
                }
            }
        };

        root.Bind(vm.DisplayedMonth, _ => RefreshAll());
        root.Bind(vm.SelectedDate, _ => RefreshAll());
        root.Bind(vm.MinDate, _ => RefreshAll());
        root.Bind(vm.MaxDate, _ => RefreshAll());
        root.Bind(vm.Culture, _ => RefreshAll());

        RefreshAll();
        return root;
    }
}
