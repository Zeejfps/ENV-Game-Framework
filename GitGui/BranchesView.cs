using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Sidebar listing local branches and remote branches (grouped per remote) as a tree —
/// branch names containing "/" are split into folder nodes (e.g. "feature/login" lives
/// inside a "feature" folder). Click a branch row to scroll/select its tip commit in the
/// history view; click a section/remote/folder row to toggle collapse. Double-click a
/// branch to check it out: local branches check out directly; remote branches that have
/// a matching local check that local out; remote branches with no matching local pop the
/// CheckoutBranchDialog. Collapse state is persisted per-repo via IRepoRegistry.
/// </summary>
public sealed class BranchesView : MultiChildView, IBranchesView
{
    private const float RowHeight = 22f;
    private const float BaseIndent = 8f;
    private const float ChevronWidth = 14f;
    private const float ChevronGap = 2f;
    private const float ChevronColumn = ChevronWidth + ChevronGap;
    private const float IconGap = 4f;
    private const float ScrollWheelStep = 60f;
    private const int DoubleClickThresholdMs = 400;

    // Ahead = "need to push", behind = "need to pull". Greenish + amber for at-a-glance.
    // Number uses the default font; icon uses the Lucide glyphs the toolbar push/pull
    // buttons use so the visual vocabulary stays consistent.
    private const uint AheadColor = 0xFF9DD17B;
    private const uint BehindColor = 0xFFE6A85C;

    private readonly TextStyle _branchTextStyle = TextStyles.Row(CommitsPalette.RowText);
    private readonly TextStyle _branchTextSelectedStyle = TextStyles.Row(CommitsPalette.RowTextActive);
    private readonly TextStyle _branchTextBusyStyle = TextStyles.Row(DialogPalette.RowTextMissing);
    private readonly TextStyle _branchIconBusyStyle = TextStyles.Icon(DialogPalette.RowTextMissing);
    private readonly TextStyle _headTextStyle = new()
    {
        TextColor = CommitsPalette.RowTextActive,
        FontWeight = FontWeight.Bold,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _headerTextStyle = TextStyles.Row(DialogPalette.SectionHeaderText);
    private readonly TextStyle _chevronStyle = new()
    {
        TextColor = DialogPalette.SectionHeaderText,
        FontSize = 8f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Center,
    };
    private readonly TextStyle _placeholderStyle = TextStyles.Centered(CommitsPalette.Placeholder);
    private readonly TextStyle _aheadNumStyle = TextStyles.Row(AheadColor);
    private readonly TextStyle _behindNumStyle = TextStyles.Row(BehindColor);
    private readonly TextStyle _aheadIconStyle = TextStyles.Icon(AheadColor);
    private readonly TextStyle _behindIconStyle = TextStyles.Icon(BehindColor);
    private readonly TextStyle _folderIconStyle = TextStyles.Icon(DialogPalette.SectionHeaderText);
    private readonly TextStyle _branchIconStyle = TextStyles.Icon(CommitsPalette.RowText);
    private readonly TextStyle _branchIconActiveStyle = TextStyles.Icon(CommitsPalette.RowTextActive);

    private IReadOnlyList<BranchRow> _rows = Array.Empty<BranchRow>();
    private BranchSelection? _selection;
    private string? _busyBranch;
    private string? _loadError;

    private float _scrollY;
    private int _hoveredRowIndex = -1;

    private bool _hasLastClick;
    private int _lastClickTickMs;
    private int _lastClickRowIndex = -1;

    public event Action<BranchRow?>? RowClicked;
    public event Action<BranchRow>? RowActivated;

    public BranchesView()
    {
        this.UseController(_ => new BranchesViewController(this));

        this.UsePresenter(ctx => new BranchesPresenter(
            this,
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>(),
            ctx.Require<State<MainViewMode>>()));
    }

    public void SetRows(IReadOnlyList<BranchRow> rows)
    {
        _rows = rows;
        _hoveredRowIndex = -1;
        if (_rows.Count == 0) _scrollY = 0f;
        ClampScroll();
    }

    public void SetSelection(BranchSelection? selection) => _selection = selection;
    public void SetBusyBranch(string? fullPath) => _busyBranch = fullPath;
    public void SetLoadError(string? error) => _loadError = error;

    private void ClampScroll()
    {
        if (Position.Height <= 0) return;
        _scrollY = ScrollMath.ClampScroll(_scrollY, _rows.Count * RowHeight, Position.Height);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var pos = Position;
        var z = GetDrawZIndex();

        c.PushClip(pos);
        c.DrawRect(new DrawRectInputs
        {
            Position = pos,
            Style = new RectStyle { BackgroundColor = CommitsPalette.Background },
            ZIndex = z,
        });

        if (_loadError != null)
        {
            c.DrawText(new DrawTextInputs
            {
                Position = pos,
                Text = "Failed to load branches: " + _loadError,
                Style = _placeholderStyle,
                ZIndex = z + 1,
            });
            c.PopClip();
            return;
        }

        if (_rows.Count == 0)
        {
            c.PopClip();
            return;
        }

        ClampScroll();

        var top = pos.Top;
        var bodyHeight = pos.Height;
        var firstVisible = Math.Max(0, (int)(_scrollY / RowHeight) - 1);
        var lastVisible = Math.Min(_rows.Count - 1, (int)((_scrollY + bodyHeight) / RowHeight) + 1);

        for (var i = firstVisible; i <= lastVisible; i++)
        {
            var row = _rows[i];
            var rowTop = top + _scrollY - i * RowHeight;
            var rowBottom = rowTop - RowHeight;
            if (rowTop <= pos.Bottom || rowBottom >= top) continue;

            DrawRow(c, pos, row, i, rowBottom, z + 1);
        }

        c.PopClip();
    }

    private void DrawRow(ICanvas c, RectF pos, BranchRow row, int rowIndex, float rowBottom, int z)
    {
        var isSelected = _selection.HasValue && _selection.Value.Matches(row);
        var isHovered = rowIndex == _hoveredRowIndex;

        var bg = isSelected
            ? CommitsPalette.RowHighlight
            : (isHovered ? DialogPalette.RowHover : (uint?)null);
        if (bg != null)
        {
            c.DrawRect(new DrawRectInputs
            {
                Position = new RectF(pos.Left, rowBottom, pos.Width, RowHeight),
                Style = new RectStyle { BackgroundColor = bg.Value },
                ZIndex = z,
            });
        }

        var contentLeft = pos.Left + BaseIndent + row.Indent;
        var rightEdge = pos.Right - 14f;

        var hasChevron = row.Kind == BranchRowKind.LocalHeader
            || row.Kind == BranchRowKind.RemotesHeader
            || row.Kind == BranchRowKind.RemoteHeader
            || row.Kind == BranchRowKind.Folder;
        var isTreeRow = row.Kind == BranchRowKind.Folder
            || row.Kind == BranchRowKind.LocalBranch
            || row.Kind == BranchRowKind.RemoteBranch;

        if (hasChevron)
        {
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(contentLeft, rowBottom, ChevronWidth, RowHeight),
                Text = row.IsOpen ? "▼" : "▶",
                Style = _chevronStyle,
                ZIndex = z + 1,
            });
            contentLeft += ChevronColumn;
        }
        else if (isTreeRow)
        {
            // Branch rows reserve the chevron column so their icons sit in the same x
            // position as a sibling folder's icon.
            contentLeft += ChevronColumn;
        }

        // Ahead/behind badge eats from the right edge before the branch name is truncated.
        if (row.Kind == BranchRowKind.LocalBranch && Context != null)
            rightEdge = DrawAheadBehindBadge(c, row, rowBottom, rightEdge, z + 1);

        if (isTreeRow && Context != null)
            contentLeft = DrawRowIcon(c, row, isSelected, contentLeft, rowBottom, z + 1);

        var textWidth = Math.Max(0f, rightEdge - contentLeft);
        if (textWidth <= 0f) return;

        var isBusy = IsBusyRow(row);
        var (text, style) = row.Kind switch
        {
            BranchRowKind.LocalHeader or BranchRowKind.RemotesHeader or BranchRowKind.RemoteHeader => (row.DisplayName, _headerTextStyle),
            BranchRowKind.LocalBranch when isBusy => (row.DisplayName, _branchTextBusyStyle),
            BranchRowKind.LocalBranch when row.IsHead => (row.DisplayName, _headTextStyle),
            _ => (row.DisplayName, isSelected ? _branchTextSelectedStyle : _branchTextStyle),
        };

        var rendered = TruncateToFit(text, style, textWidth);
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(contentLeft, rowBottom, textWidth, RowHeight),
            Text = rendered,
            Style = style,
            ZIndex = z + 1,
        });
    }

    private float DrawRowIcon(ICanvas c, BranchRow row, bool isSelected, float left, float rowBottom, int z)
    {
        string glyph;
        TextStyle style;
        if (row.Kind == BranchRowKind.Folder)
        {
            glyph = row.IsOpen ? LucideIcons.FolderOpen : LucideIcons.Folder;
            style = _folderIconStyle;
        }
        else
        {
            glyph = LucideIcons.Branch;
            style = IsBusyRow(row)
                ? _branchIconBusyStyle
                : ((row.IsHead || isSelected) ? _branchIconActiveStyle : _branchIconStyle);
        }

        var width = Context!.Canvas.MeasureTextWidth(glyph, style);
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(left, rowBottom, width, RowHeight),
            Text = glyph,
            Style = style,
            ZIndex = z,
        });
        return left + width + IconGap;
    }

    // Returns the new right-edge after drawing the badge so the name's truncation knows
    // how much room is left.
    private float DrawAheadBehindBadge(ICanvas c, BranchRow row, float rowBottom, float rightEdge, int z)
    {
        var ahead = row.AheadBy.GetValueOrDefault();
        var behind = row.BehindBy.GetValueOrDefault();
        if (ahead == 0 && behind == 0) return rightEdge;

        const float badgeGap = 8f;
        const float numIconGap = 3f;
        var cursor = rightEdge;

        if (behind > 0)
            cursor = DrawCountAndIcon(c, behind.ToString(), LucideIcons.Pull, _behindNumStyle, _behindIconStyle, cursor, rowBottom, numIconGap, z) - badgeGap;
        if (ahead > 0)
            cursor = DrawCountAndIcon(c, ahead.ToString(), LucideIcons.Push, _aheadNumStyle, _aheadIconStyle, cursor, rowBottom, numIconGap, z) - badgeGap;

        return cursor;
    }

    // Draws "<count><gap><icon>" right-aligned to <rightX>. Returns the left edge of the
    // drawn pair so callers can chain badges leftward.
    private float DrawCountAndIcon(
        ICanvas c, string count, string icon,
        TextStyle countStyle, TextStyle iconStyle,
        float rightX, float rowBottom, float gap, int z)
    {
        var canvas = Context!.Canvas;
        var iconWidth = canvas.MeasureTextWidth(icon, iconStyle);
        var countWidth = canvas.MeasureTextWidth(count, countStyle);
        var iconLeft = rightX - iconWidth;
        var countLeft = iconLeft - gap - countWidth;

        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(countLeft, rowBottom, countWidth, RowHeight),
            Text = count,
            Style = countStyle,
            ZIndex = z,
        });
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(iconLeft, rowBottom, iconWidth, RowHeight),
            Text = icon,
            Style = iconStyle,
            ZIndex = z,
        });
        return countLeft;
    }

    private bool IsBusyRow(BranchRow row) =>
        _busyBranch != null
        && row.Kind == BranchRowKind.LocalBranch
        && row.FullPath != null
        && row.FullPath == _busyBranch;

    private string TruncateToFit(string text, TextStyle style, float available)
    {
        if (Context == null) return text;
        return TextMeasure.TruncateToFit(text, style, available, Context.Canvas);
    }

    internal void OnWheel(float deltaY)
    {
        if (_rows.Count == 0) return;
        _scrollY -= deltaY * ScrollWheelStep;
        ClampScroll();
    }

    internal void SetHover(PointF point)
    {
        var idx = HitTestRow(point);
        if (idx == _hoveredRowIndex) return;
        _hoveredRowIndex = idx;
    }

    internal void ClearHover()
    {
        if (_hoveredRowIndex < 0) return;
        _hoveredRowIndex = -1;
    }

    internal void OnClickAt(PointF point)
    {
        // Click in the panel but outside any row → deselect (and clear CommitsView too).
        if (!IsPointInside(point)) return;
        var idx = HitTestRow(point);
        var row = (idx >= 0 && idx < _rows.Count) ? _rows[idx] : null;

        RowClicked?.Invoke(row);

        // Double-click detection sits on top of single-click: the first click of a
        // double-click still selects and jumps to history (matches Fork), the second
        // adds the activate (checkout) on top.
        if (row == null)
        {
            _hasLastClick = false;
            return;
        }

        var now = Environment.TickCount;
        var isDouble = _hasLastClick
            && _lastClickRowIndex == idx
            && unchecked(now - _lastClickTickMs) <= DoubleClickThresholdMs;
        if (isDouble)
        {
            RowActivated?.Invoke(row);
            _hasLastClick = false;
        }
        else
        {
            _lastClickTickMs = now;
            _lastClickRowIndex = idx;
            _hasLastClick = true;
        }
    }

    private bool IsPointInside(PointF point)
    {
        var pos = Position;
        return point.X >= pos.Left && point.X <= pos.Right
            && point.Y >= pos.Bottom && point.Y <= pos.Top;
    }

    private int HitTestRow(PointF point)
    {
        var pos = Position;
        if (point.X < pos.Left || point.X > pos.Right) return -1;
        if (point.Y < pos.Bottom || point.Y > pos.Top) return -1;
        if (_rows.Count == 0) return -1;

        var distFromTop = pos.Top - point.Y;
        var idx = (int)((distFromTop + _scrollY) / RowHeight);
        if (idx < 0 || idx >= _rows.Count) return -1;
        return idx;
    }
}
