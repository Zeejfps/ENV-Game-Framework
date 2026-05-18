using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Inline warning banner: a bordered box with red-on-amber text. Hidden by being
/// absent from its parent's child list (parents using a Gap-aware container won't
/// reserve space when the bar is hidden); shown by being inserted at <c>InsertAt</c>.
/// </summary>
internal sealed class ErrorBar
{
    public RectView View { get; }
    private readonly TextView _text;
    private readonly MultiChildView _container;
    private readonly int _insertAt;

    /// <param name="container">Parent the bar is added to / removed from.</param>
    /// <param name="insertAt">Index to insert at; -1 appends.</param>
    /// <param name="verticalPadding">Internal vertical padding; defaults to 4.</param>
    public ErrorBar(MultiChildView container, int insertAt = -1, int verticalPadding = 4)
    {
        _container = container;
        _insertAt = insertAt;
        _text = new TextView
        {
            TextColor = CommitsPalette.WarningText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        View = new RectView
        {
            BackgroundColor = CommitsPalette.WarningBg,
            BorderColor = BorderColorStyle.All(CommitsPalette.WarningBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = 8,
                Right = 8,
                Top = verticalPadding,
                Bottom = verticalPadding,
            },
            Children = { _text },
        };
    }

    /// <summary>Show with <paramref name="message"/>, or hide when null.</summary>
    public string? Message
    {
        set
        {
            if (value == null)
            {
                _container.Children.Remove(View);
                return;
            }
            _text.Text = value;
            if (!_container.Children.Contains(View))
            {
                if (_insertAt < 0) _container.Children.Add(View);
                else _container.Children.Insert(_insertAt, View);
            }
        }
    }
}
