using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class CommitsPanelView : MultiChildView
{
    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;

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

        AddChildToSelf(new BorderLayoutView
        {
            Center = _commits,
            East = _scrollBar,
        });

        _scrollBar.Behaviors.Add(new VerticalScrollBarViewController(_scrollBar));
        Behaviors.Add(new CommitsPanelController(_commits, _scrollBar));
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        _scrollBar.Scale = _commits.Scale;
    }
}

internal sealed class CommitsPanelController : KeyboardMouseController
{
    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;

    public CommitsPanelController(CommitsView commits, VerticalScrollBarView scrollBar)
    {
        _commits = commits;
        _scrollBar = scrollBar;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _commits.ScrollPositionChanged += OnCommitsScrollChanged;
        _commits.ScaleChanged += OnCommitsScaleChanged;
        _scrollBar.ScrollPositionChanged += OnScrollBarScrollChanged;
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _commits.ScrollPositionChanged -= OnCommitsScrollChanged;
        _commits.ScaleChanged -= OnCommitsScaleChanged;
        _scrollBar.ScrollPositionChanged -= OnScrollBarScrollChanged;
    }

    private void OnCommitsScrollChanged(float normalized)
    {
        _scrollBar.SetNormalizedScrollPosition(normalized);
    }

    private void OnCommitsScaleChanged(float scale)
    {
        _scrollBar.Scale = scale;
    }

    private void OnScrollBarScrollChanged(float normalized)
    {
        _commits.SetNormalizedScrollPosition(normalized);
    }
}
