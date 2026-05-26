using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

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
public sealed class DiffView : MultiChildView, IBind<DiffViewModel>
{
    // Height of the always-visible header strip. Exposed so the parent split container
    // can pin the bottom panel to exactly this height when the diff is collapsed, so the
    // chevron stays clickable even when the body is hidden.
    public const float HeaderHeight = 24f;

    private readonly State<DiffTarget?> _target = new(null);
    private readonly State<bool> _isCollapsed = new(false);

    private readonly DiffContentView _content;

    public DiffView()
    {
        _content = new DiffContentView();
        var vScrollBar = ScrollBarStyles.CreateVertical();
        var hScrollBar = ScrollBarStyles.CreateHorizontal();

        var body = new BorderLayoutView
        {
            Center = _content,
            East = vScrollBar,
            South = hScrollBar,
        };

        // Outer layout: header always on top, body in the center. When collapsed, we
        // null out Center so the body's hScrollBar isn't laid out — otherwise its
        // South=hScrollBar measures at its natural height and draws over the header
        // (body draws after header in z-order, since it's added second).
        var outerLayout = new BorderLayoutView
        {
            North = BuildHeaderBar(),
            Center = body,
        };
        _isCollapsed.Subscribe(c => outerLayout.Center = c ? null : body);

        var frame = new RectView { Children = { outerLayout } };
        frame.BindBackgroundColorFromTheme(t => t.Commits.Background);
        AddChildToSelf(frame);

        this.UseController(_ => new ScrollSyncController(_content, vScrollBar, hScrollBar));
        this.UseViewModel(
            ctx => new DiffViewModel(
                _target,
                ctx.Require<IRepoRegistry>(),
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>()),
            Bind);
    }

    public IReadable<DiffTarget?> Target => _target;
    public IReadable<bool> IsCollapsed => _isCollapsed;

    public void SetTarget(string? path, DiffSide side, string? commitSha = null)
    {
        _target.Value = path == null ? null : new DiffTarget(path, side, commitSha);
    }

    public void Bind(DiffViewModel vm)
    {
        vm.RenderState.Subscribe(_content.SetRenderState);
    }

    private View BuildHeaderBar()
    {
        var hovered = new State<bool>(false);

        // Theme-reactive color states updated by the BindToTheme behavior below. The hover
        // bindings on the title / chevron / bar read from these via Derived, so a theme swap
        // OR a hover state change refreshes the chrome.
        var titleIdle = new State<uint>(ThemePresets.Dark.Text.Row);
        var titleHover = new State<uint>(ThemePresets.Dark.Text.Strong);
        var barIdleBg = new State<uint>(ThemePresets.Dark.FileChanges.HeaderBg);
        var barHoverBg = new State<uint>(ThemePresets.Dark.Dialog.ButtonHover);
        var topBorder = new State<uint>(ThemePresets.Dark.Commits.Border);
        var bottomBorder = new State<uint>(ThemePresets.Dark.FileChanges.HeaderBorder);
        this.BindToTheme(t =>
        {
            titleIdle.Value = t.Text.Row;
            titleHover.Value = t.Text.Strong;
            barIdleBg.Value = t.FileChanges.HeaderBg;
            barHoverBg.Value = t.Dialog.ButtonHover;
            topBorder.Value = t.Commits.Border;
            bottomBorder.Value = t.FileChanges.HeaderBorder;
        });

        var title = new TextView
        {
            Text = "Diff View",
            FontSize = 12f,
            VerticalTextAlignment = TextAlignment.Center,
        };
        title.BindTextColor(() => hovered.Value ? titleHover.Value : titleIdle.Value);

        var chevron = new TextView
        {
            FontFamily = LucideIcons.FontFamily,
            FontSize = 12f,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16f,
        };
        chevron.BindText(_isCollapsed, c => c ? LucideIcons.ChevronUp : LucideIcons.ChevronDown);
        chevron.BindTextColor(() => hovered.Value ? titleHover.Value : titleIdle.Value);

        var bar = new RectView
        {
            PreferredHeight = HeaderHeight,
            BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
            Padding = new PaddingStyle { Left = 8, Right = 6 },
            Children =
            {
                new FlexRowView
                {
                    Gap = 4f,
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children =
                    {
                        new FlexItem { Grow = 1, Child = title },
                        chevron,
                    },
                },
            },
        };
        bar.BindBorderColor(() => new BorderColorStyle
        {
            Top = topBorder.Value,
            Bottom = bottomBorder.Value,
        });
        bar.BindBackgroundColor(() => hovered.Value ? barHoverBg.Value : barIdleBg.Value);

        bar.UseController(_ => new HoverableButtonController(
            () => _isCollapsed.Value = !_isCollapsed.Value,
            h => hovered.Value = h));

        return bar;
    }
}
