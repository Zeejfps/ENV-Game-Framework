using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Sidebar content for local branches, remotes, tags, and stashes. Placeholder content
/// for now — the tree of refs is filled in later. Width is managed by the surrounding
/// <see cref="ResizableLeftSidebar"/>; this view just fills whatever it's given.
/// </summary>
public sealed class BranchesView : MultiChildView
{
    public BranchesView()
    {
        var placeholder = new TextView
        {
            Text = "Branches & Remotes",
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children = { placeholder },
        });
    }
}

/// <summary>
/// Wraps a sidebar content view and lays a draggable splitter strip on its right edge.
/// The wrapper itself owns the sidebar's width via <see cref="View.PreferredWidth"/>, so
/// the surrounding <c>BorderLayoutView</c> reads the new width on each layout pass after
/// a drag. Width is clamped to <see cref="MinWidth"/> / <see cref="MaxWidth"/>.
/// </summary>
internal sealed class ResizableLeftSidebar : MultiChildView
{
    private const float SplitterThickness = 5f;

    private readonly View _content;
    private readonly View _splitter;
    private readonly float _minWidth;
    private readonly float _maxWidth;

    public ResizableLeftSidebar(View content, View splitter, float initialWidth, float minWidth, float maxWidth)
    {
        _content = content;
        _splitter = splitter;
        _minWidth = minWidth;
        _maxWidth = maxWidth;
        PreferredWidth = Math.Clamp(initialWidth, _minWidth, _maxWidth);
        AddChildToSelf(_content);
        AddChildToSelf(_splitter);
    }

    // Positive dx = mouse moved right = sidebar grows. Negative shrinks. Clamping keeps
    // the sidebar usable at both extremes (it can't disappear or eat the main view).
    public void AdjustWidthByPixels(float dx)
    {
        var newWidth = Math.Clamp((float)PreferredWidth + dx, _minWidth, _maxWidth);
        if (Math.Abs(newWidth - (float)PreferredWidth) < 0.5f) return;
        PreferredWidth = newWidth;
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        if (pos.Width <= 0f || pos.Height <= 0f) return;

        var contentWidth = Math.Max(0f, pos.Width - SplitterThickness);

        _content.LeftConstraint = pos.Left;
        _content.BottomConstraint = pos.Bottom;
        _content.MinWidthConstraint = contentWidth;
        _content.MaxWidthConstraint = contentWidth;
        _content.MaxHeightConstraint = pos.Height;
        _content.LayoutSelf();

        _splitter.LeftConstraint = pos.Left + contentWidth;
        _splitter.BottomConstraint = pos.Bottom;
        _splitter.MinWidthConstraint = SplitterThickness;
        _splitter.MaxWidthConstraint = SplitterThickness;
        _splitter.MaxHeightConstraint = pos.Height;
        _splitter.LayoutSelf();
    }

    /// <summary>
    /// Convenience factory: builds the sidebar wrapper, the splitter rect (with hover
    /// styling), and wires the splitter controller in one place. Returns the wrapper.
    /// </summary>
    public static ResizableLeftSidebar Build(
        View content,
        float initialWidth,
        float minWidth = 140f,
        float maxWidth = 600f)
    {
        var splitterHovered = new State<bool>(false);
        var splitter = new RectView();
        splitter.BindBackgroundColor(splitterHovered,
            h => h ? CommitsPalette.DividerHoverBg : CommitsPalette.Border);

        var sidebar = new ResizableLeftSidebar(content, splitter, initialWidth, minWidth, maxWidth);

        splitter.Behaviors.Add(new SplitterController(
            DragAxis.X,
            sidebar.AdjustWidthByPixels,
            h => splitterHovered.Value = h));

        return sidebar;
    }
}
