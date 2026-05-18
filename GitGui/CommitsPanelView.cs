using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class CommitsPanelView : MultiChildView
{
    private const float WarningBarHeight = 24f;

    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly RectView _warningBar;
    private readonly TextView _warningText;

    public CommitsPanelView()
    {
        _commits = new CommitsView();
        _scrollBar = new VerticalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        _scrollBar.Thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        _scrollBar.Thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        _scrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        _scrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);

        _warningText = new TextView
        {
            TextColor = CommitsPalette.WarningText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _warningBar = new RectView
        {
            PreferredHeight = 0f,
            Children = { _warningText },
        };

        AddChildToSelf(new BorderLayoutView
        {
            Center = _commits,
            East = _scrollBar,
            South = _warningBar,
        });

        _scrollBar.Behaviors.Add(new VerticalScrollBarViewController(_scrollBar));
        Behaviors.Add(new CommitsPanelController(_commits, _scrollBar, _warningBar, _warningText));
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        var scale = _commits.Scale;
        _scrollBar.PreferredWidth = scale < 1f ? ScrollBarSync.Thickness : 0f;
        _scrollBar.Scale = scale;
    }

    internal static float WarningBarShownHeight => WarningBarHeight;
}

internal sealed class CommitsPanelController : KeyboardMouseController
{
    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly RectView _warningBar;
    private readonly TextView _warningText;

    public CommitsPanelController(CommitsView commits, VerticalScrollBarView scrollBar, RectView warningBar, TextView warningText)
    {
        _commits = commits;
        _scrollBar = scrollBar;
        _warningBar = warningBar;
        _warningText = warningText;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _commits.ScrollPositionChanged += OnCommitsScrollChanged;
        _commits.ScaleChanged += OnCommitsScaleChanged;
        _commits.TruncatedChanged += OnTruncatedChanged;
        _scrollBar.ScrollPositionChanged += OnScrollBarScrollChanged;
        OnTruncatedChanged(_commits.Truncated);
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _commits.ScrollPositionChanged -= OnCommitsScrollChanged;
        _commits.ScaleChanged -= OnCommitsScaleChanged;
        _commits.TruncatedChanged -= OnTruncatedChanged;
        _scrollBar.ScrollPositionChanged -= OnScrollBarScrollChanged;
    }

    private void OnCommitsScrollChanged(float normalized)
    {
        _scrollBar.SetNormalizedScrollPosition(normalized);
    }

    private void OnCommitsScaleChanged(float scale)
    {
        _scrollBar.PreferredWidth = scale < 1f ? ScrollBarSync.Thickness : 0f;
        _scrollBar.Scale = scale;
    }

    private void OnScrollBarScrollChanged(float normalized)
    {
        _commits.SetNormalizedScrollPosition(normalized);
    }

    private void OnTruncatedChanged(bool truncated)
    {
        _warningBar.PreferredHeight = truncated ? CommitsPanelView.WarningBarShownHeight : 0f;
        _warningBar.BackgroundColor = truncated ? CommitsPalette.WarningBg : 0u;
        _warningBar.BorderColor = truncated
            ? new BorderColorStyle { Top = CommitsPalette.WarningBorder }
            : new BorderColorStyle();
        _warningBar.BorderSize = truncated
            ? new BorderSizeStyle { Top = 1 }
            : new BorderSizeStyle();
        _warningText.Text = truncated ? "History truncated." : null;
    }
}
