using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Bottom-of-window status bar showing in-flight git ops (fetch, push, fast-forward, ...).
/// The bar is hidden (detached from its parent) when no ops are active so it doesn't take
/// vertical space at rest. <see cref="OperationsStatusBarPresenter"/> drives the contents
/// via <see cref="AddRow"/> / <see cref="RemoveRow"/> and toggles the log panel via
/// <see cref="ShowLog"/> / <see cref="HideLog"/>.
/// </summary>
internal sealed class OperationsStatusBar
{
    public RectView View { get; }

    private readonly MultiChildView _container;
    private readonly FlexColumnView _rows;
    private readonly RectView _logPanel;
    private readonly TextView _logText;
    private bool _logVisible;

    public OperationsStatusBar(MultiChildView container)
    {
        _container = container;

        _rows = new FlexColumnView
        {
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };

        _logText = new TextView
        {
            TextColor = Theme.TextRow,
            FontFamily = DiffOptions.MonoFontFamily,
            FontSize = 11,
            TextWrap = TextWrap.Wrap,
            VerticalTextAlignment = TextAlignment.Start,
        };

        _logPanel = new RectView
        {
            BackgroundColor = Theme.BgDeep,
            BorderColor = new BorderColorStyle { Bottom = Theme.Border },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle { Left = 12, Right = 12, Top = 6, Bottom = 6 },
            PreferredHeight = 160f,
            Children = { _logText },
        };

        View = new RectView
        {
            BackgroundColor = Theme.BgHeader,
            BorderColor = new BorderColorStyle { Top = Theme.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Children =
            {
                new FlexColumnView
                {
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children = { _rows },
                },
            },
        };
    }

    public bool IsVisible => _container.Children.Contains(View);

    public void AddRow(OperationRow row)
    {
        _rows.Children.Add(row);
        EnsureAttached();
    }

    public void RemoveRow(OperationRow row)
    {
        _rows.Children.Remove(row);
        if (_rows.Children.Count == 0)
        {
            HideLog();
            _container.Children.Remove(View);
        }
    }

    public void ShowLog(IReadOnlyList<string> lines)
    {
        _logText.Text = lines.Count == 0 ? "(no output yet)" : string.Join('\n', lines);
        if (_logVisible) return;
        _logVisible = true;
        // Insert log panel above the rows column inside the bar's inner column.
        var inner = (FlexColumnView)View.Children[0];
        inner.Children.Insert(0, _logPanel);
    }

    public void UpdateLog(IReadOnlyList<string> lines)
    {
        if (!_logVisible) return;
        _logText.Text = lines.Count == 0 ? "(no output yet)" : string.Join('\n', lines);
    }

    public void HideLog()
    {
        if (!_logVisible) return;
        _logVisible = false;
        var inner = (FlexColumnView)View.Children[0];
        inner.Children.Remove(_logPanel);
    }

    private void EnsureAttached()
    {
        if (_container.Children.Contains(View)) return;
        _container.Children.Add(View);
    }
}
