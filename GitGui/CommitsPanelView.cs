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

    private ThemeTokens _tokens = ThemePresets.Dark;
    private bool _truncated;

    public CommitsPanelView()
    {
        _commits = new CommitsView();
        _scrollBar = new VerticalScrollBarView
        {
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        _scrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);

        _warningText = new TextView
        {
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

        _scrollBar.UseController(_ => new VerticalScrollBarViewController(_scrollBar));
        this.UseController(_ => new CommitsPanelController(_commits, _scrollBar, this));
        this.BindToTheme(RebuildVisuals);
    }

    private void RebuildVisuals(ThemeTokens tokens)
    {
        _tokens = tokens;
        var c = tokens.Commits;
        _scrollBar.TrackBackgroundColor = c.ScrollTrackBg;
        _scrollBar.TrackBorderColor = new BorderColorStyle
        {
            Left = c.ScrollTrackBorder,
            Top = c.ScrollTrackBorder,
            Right = c.ScrollTrackBorder,
            Bottom = c.ScrollTrackBorder,
        };
        _scrollBar.Thumb.IdleBackgroundColor = c.ScrollThumbBg;
        _scrollBar.Thumb.HoveredBackgroundColor = c.ScrollThumbHoverBg;
        _scrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = c.ScrollThumbBorder,
            Top = c.ScrollThumbBorder,
            Right = c.ScrollThumbBorder,
            Bottom = c.ScrollThumbBorder,
        };
        _warningText.TextColor = c.WarningText;
        ApplyWarningBarChrome();
    }

    internal void SetTruncated(bool truncated)
    {
        if (_truncated == truncated) return;
        _truncated = truncated;
        _warningBar.PreferredHeight = truncated ? WarningBarShownHeight : 0f;
        _warningText.Text = truncated ? "History truncated." : null;
        ApplyWarningBarChrome();
    }

    private void ApplyWarningBarChrome()
    {
        if (_truncated)
        {
            _warningBar.BackgroundColor = _tokens.Commits.WarningBg;
            _warningBar.BorderColor = new BorderColorStyle { Top = _tokens.Commits.WarningBorder };
            _warningBar.BorderSize = new BorderSizeStyle { Top = 1 };
        }
        else
        {
            _warningBar.BackgroundColor = 0u;
            _warningBar.BorderColor = new BorderColorStyle();
            _warningBar.BorderSize = new BorderSizeStyle();
        }
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

internal sealed class CommitsPanelController : KeyboardMouseController, IDisposable
{
    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly CommitsPanelView _panel;

    public CommitsPanelController(CommitsView commits, VerticalScrollBarView scrollBar, CommitsPanelView panel)
    {
        _commits = commits;
        _scrollBar = scrollBar;
        _panel = panel;

        _commits.ScrollPositionChanged += OnCommitsScrollChanged;
        _commits.ScaleChanged += OnCommitsScaleChanged;
        _commits.TruncatedChanged += OnTruncatedChanged;
        _scrollBar.ScrollPositionChanged += OnScrollBarScrollChanged;
        OnTruncatedChanged(_commits.Truncated);
    }

    public void Dispose()
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

    private void OnTruncatedChanged(bool truncated) => _panel.SetTruncated(truncated);
}
