using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

public sealed class CommitsPanelView : MultiChildView
{
    private const float WarningBarHeight = 24f;

    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly RectView _warningBar;
    private readonly TextView _warningText;
    private readonly State<bool> _truncated = new(false);

    public CommitsPanelView()
    {
        _commits = new CommitsView();
        _scrollBar = ScrollBars.CreateVertical();

        _warningText = new TextView
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _warningText.BindThemedTextColor(s => s.CommitsTruncationBar.Text);
        _warningText.BindText(_truncated, t => t ? "History truncated." : null);

        _warningBar = new RectView
        {
            PreferredHeight = 0f,
            Children = { _warningText },
        };
        _warningBar.BindThemedBackgroundColor(s =>
            _truncated.Value ? s.CommitsTruncationBar.Background : 0u);
        _warningBar.BindThemedBorderColor(s => _truncated.Value
            ? new BorderColorStyle { Top = s.CommitsTruncationBar.BorderTop }
            : new BorderColorStyle());
        _truncated.Subscribe(t =>
        {
            _warningBar.PreferredHeight = t ? WarningBarHeight : 0f;
            _warningBar.BorderSize = t ? new BorderSizeStyle { Top = 1 } : new BorderSizeStyle();
        });

        AddChildToSelf(new BorderLayoutView
        {
            Center = _commits,
            East = _scrollBar,
            South = _warningBar,
        });

        this.UseController(_ => new CommitsPanelController(_commits, _scrollBar, _truncated));
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        var scale = _commits.Scale;
        _scrollBar.PreferredWidth = scale < 1f ? ScrollBarSync.Thickness : 0f;
        _scrollBar.Scale = scale;
    }
}

internal sealed class CommitsPanelController : KeyboardMouseController, IDisposable
{
    private readonly CommitsView _commits;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly State<bool> _truncated;

    public CommitsPanelController(CommitsView commits, VerticalScrollBarView scrollBar, State<bool> truncated)
    {
        _commits = commits;
        _scrollBar = scrollBar;
        _truncated = truncated;

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

    private void OnTruncatedChanged(bool truncated) => _truncated.Value = truncated;
}
