using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public abstract record DiffViewModel
{
    public sealed record Placeholder(string Text) : DiffViewModel;
    public sealed record Loaded(DiffResult Result) : DiffViewModel;
}

/// <summary>
/// Diff panel shown below the file lists in Local Changes whenever exactly one file is
/// selected. <see cref="SetTarget"/> drives the load; the panel renders banners, hunk
/// separators, and per-line gutter+glyph+text rows for the resulting <see cref="DiffResult"/>.
/// </summary>
/// <remarks>
/// Rendering is virtualized — only rows intersecting the viewport are drawn (see
/// <see cref="DiffContentView"/>). The previous implementation materialized one
/// <c>RectView</c>+<c>FlexRowView</c>+4×<c>TextView</c> per line into a <c>ColumnView</c>
/// inside a <c>ScrollPane</c>, which forced O(N) text measurement on every layout pass for
/// diffs of 5000 lines.
/// </remarks>
public sealed class DiffView : MultiChildView, IDiffView
{
    private readonly State<DiffTarget?> _target = new(null);

    private readonly DiffContentView _content;

    public DiffView()
    {
        _content = new DiffContentView();
        var vScrollBar = ScrollBarStyles.CreateVertical();
        var hScrollBar = ScrollBarStyles.CreateHorizontal();

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            BorderColor = new BorderColorStyle { Top = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Children =
            {
                new BorderLayoutView
                {
                    Center = _content,
                    East = vScrollBar,
                    South = hScrollBar,
                },
            },
        });

        this.UseController(_ => new ScrollSyncController(_content, vScrollBar, hScrollBar));
        this.UsePresenter(ctx => new DiffPresenter(
            this,
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>()));
    }

    public IReadable<DiffTarget?> Target => _target;

    public void SetViewModel(DiffViewModel vm) => _content.SetViewModel(vm);

    public void SetTarget(string? path, DiffSide side)
    {
        _target.Value = path == null ? null : new DiffTarget(path, side);
    }
}