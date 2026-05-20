using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Sidebar listing local branches and remote branches (grouped per remote) as a tree —
/// branch names containing "/" are split into folder nodes (e.g. "feature/login" lives
/// inside a "feature" folder). Click a branch row to scroll/select its tip commit in the
/// history view; click a section/remote/folder row to toggle collapse. Double-click a
/// branch to check it out: local branches check out directly; remote branches that have
/// a matching local check that local out; remote branches with no matching local pop the
/// CheckoutBranchDialog. Right-click a local/remote branch row to open a context menu
/// (Checkout / Rename / Delete). Collapse state is persisted per-repo via IRepoRegistry.
/// </summary>
public sealed class BranchesView : MultiChildView
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
    private bool _isLoading;

    // Latest values from the VM, used to recompute _rows whenever either changes.
    // Stored as fields rather than rebuilding from inside each subscription so a paired
    // Listing+Ui change still produces a consistent row list on each fire.
    private BranchListing? _listing;
    private BranchesUiState _ui = new();

    private float _scrollY;
    private int _hoveredRowIndex = -1;
    // Tracks which row currently has an open right-click context menu, drawn with the
    // same hover background. Distinct from _hoveredRowIndex because the pointer often
    // leaves the row while the menu is open.
    private int _contextHighlightRowIndex = -1;

    private bool _hasLastClick;
    private int _lastClickTickMs;
    private int _lastClickRowIndex = -1;

    private BranchesViewModel? _vm;

    public BranchesView()
    {
        this.UseController(ctx => new BranchesViewController(this, ctx));

        this.UsePresenter(ctx =>
        {
            var vm = new BranchesViewModel(
                ctx.Require<IRepoRegistry>(),
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>(),
                ctx.Require<State<MainViewMode>>());
            Bind(vm);
            return vm;
        });
    }

    private void Bind(BranchesViewModel vm)
    {
        _vm = vm;
        vm.Listing.Subscribe(listing => { _listing = listing; RebuildRows(); });
        vm.Ui.Subscribe(ui => { _ui = ui; RebuildRows(); });
        vm.Selection.Subscribe(SetSelection);
        vm.BusyBranch.Subscribe(SetBusyBranch);
        vm.LoadError.Subscribe(SetLoadError);
        vm.IsLoading.Subscribe(SetIsLoading);
    }

    private void RebuildRows()
    {
        _rows = BuildRows(_listing, _ui);
        _hoveredRowIndex = -1;
        _contextHighlightRowIndex = -1;
        if (_rows.Count == 0) _scrollY = 0f;
        ClampScroll();
    }

    private void SetSelection(BranchSelection? selection) => _selection = selection;
    private void SetBusyBranch(string? fullPath) => _busyBranch = fullPath;
    private void SetLoadError(string? error) => _loadError = error;
    private void SetIsLoading(bool isLoading) => _isLoading = isLoading;

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
            if (_isLoading)
            {
                c.DrawText(new DrawTextInputs
                {
                    Position = pos,
                    Text = "Loading…",
                    Style = _placeholderStyle,
                    ZIndex = z + 1,
                });
            }
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
        var isContextHighlighted = rowIndex == _contextHighlightRowIndex;

        var bg = isSelected
            ? CommitsPalette.RowHighlight
            : ((isHovered || isContextHighlighted) ? DialogPalette.RowHover : (uint?)null);
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
            || row.Kind == BranchRowKind.StashesHeader
            || row.Kind == BranchRowKind.Folder;
        var isTreeRow = row.Kind == BranchRowKind.Folder
            || row.Kind == BranchRowKind.LocalBranch
            || row.Kind == BranchRowKind.RemoteBranch
            || row.Kind == BranchRowKind.Stash;

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

        if (isTreeRow && Context != null)
            contentLeft = DrawRowIcon(c, row, isSelected, contentLeft, rowBottom, z + 1);

        const float nameBadgeGap = 8f;
        var badgeWidth = (row.Kind == BranchRowKind.LocalBranch && Context != null)
            ? MeasureAheadBehindBadge(row)
            : 0f;
        var nameBudget = Math.Max(0f, rightEdge - contentLeft - (badgeWidth > 0 ? badgeWidth + nameBadgeGap : 0f));
        if (nameBudget <= 0f) return;

        var isBusy = IsBusyRow(row);
        var (text, style) = row.Kind switch
        {
            BranchRowKind.LocalHeader or BranchRowKind.RemotesHeader or BranchRowKind.RemoteHeader or BranchRowKind.StashesHeader => (row.DisplayName, _headerTextStyle),
            BranchRowKind.LocalBranch when isBusy => (row.DisplayName, _branchTextBusyStyle),
            BranchRowKind.LocalBranch when row.IsHead => (row.DisplayName, _headTextStyle),
            _ => (row.DisplayName, isSelected ? _branchTextSelectedStyle : _branchTextStyle),
        };

        var rendered = TruncateToFit(text, style, nameBudget);
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(contentLeft, rowBottom, nameBudget, RowHeight),
            Text = rendered,
            Style = style,
            ZIndex = z + 1,
        });

        if (badgeWidth > 0)
        {
            var nameWidth = Context!.Canvas.MeasureTextWidth(rendered, style);
            DrawAheadBehindBadgeAt(c, row, contentLeft + nameWidth + nameBadgeGap, rowBottom, z + 1);
        }
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
        else if (row.Kind == BranchRowKind.Stash)
        {
            glyph = LucideIcons.Stash;
            style = isSelected ? _branchIconActiveStyle : _branchIconStyle;
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

    private const float BadgeGap = 8f;
    private const float BadgeNumIconGap = 0f;

    private float MeasureAheadBehindBadge(BranchRow row)
    {
        var ahead = row.AheadBy.GetValueOrDefault();
        var behind = row.BehindBy.GetValueOrDefault();
        if (ahead == 0 && behind == 0) return 0f;

        var canvas = Context!.Canvas;
        var width = 0f;
        if (ahead > 0)
        {
            width += canvas.MeasureTextWidth(LucideIcons.Push, _aheadIconStyle)
                   + BadgeNumIconGap
                   + canvas.MeasureTextWidth(ahead.ToString(), _aheadNumStyle);
        }
        if (behind > 0)
        {
            if (width > 0) width += BadgeGap;
            width += canvas.MeasureTextWidth(LucideIcons.Pull, _behindIconStyle)
                   + BadgeNumIconGap
                   + canvas.MeasureTextWidth(behind.ToString(), _behindNumStyle);
        }
        return width;
    }

    private void DrawAheadBehindBadgeAt(ICanvas c, BranchRow row, float leftX, float rowBottom, int z)
    {
        var ahead = row.AheadBy.GetValueOrDefault();
        var behind = row.BehindBy.GetValueOrDefault();
        if (ahead == 0 && behind == 0) return;

        var cursor = leftX;
        if (ahead > 0)
            cursor = DrawIconAndCount(c, ahead.ToString(), LucideIcons.Push, _aheadNumStyle, _aheadIconStyle, cursor, rowBottom, BadgeNumIconGap, z) + BadgeGap;
        if (behind > 0)
            DrawIconAndCount(c, behind.ToString(), LucideIcons.Pull, _behindNumStyle, _behindIconStyle, cursor, rowBottom, BadgeNumIconGap, z);
    }

    // Draws "<icon><gap><count>" left-aligned at <leftX>. Returns the right edge of the
    // drawn pair so callers can chain badges rightward.
    private float DrawIconAndCount(
        ICanvas c, string count, string icon,
        TextStyle countStyle, TextStyle iconStyle,
        float leftX, float rowBottom, float gap, int z)
    {
        var canvas = Context!.Canvas;
        var iconWidth = canvas.MeasureTextWidth(icon, iconStyle);
        var countWidth = canvas.MeasureTextWidth(count, countStyle);
        var countLeft = leftX + iconWidth + gap;

        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(leftX, rowBottom, iconWidth, RowHeight),
            Text = icon,
            Style = iconStyle,
            ZIndex = z,
        });
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(countLeft, rowBottom, countWidth, RowHeight),
            Text = count,
            Style = countStyle,
            ZIndex = z,
        });
        return countLeft + countWidth;
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
        _hoveredRowIndex = idx;
    }

    internal void ClearHover()
    {
        if (_hoveredRowIndex < 0) return;
        _hoveredRowIndex = -1;
    }

    internal void OnClickAt(PointF point)
    {
        if (!Position.ContainsPoint(point)) return;
        if (_vm == null) return;

        var idx = HitTestRow(point);
        var row = (idx >= 0 && idx < _rows.Count) ? _rows[idx] : null;

        DispatchClick(_vm, row);

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
            DispatchActivate(_vm, row);
            _hasLastClick = false;
        }
        else
        {
            _lastClickTickMs = now;
            _lastClickRowIndex = idx;
            _hasLastClick = true;
        }
    }

    private static void DispatchClick(BranchesViewModel vm, BranchRow? row)
    {
        if (row == null)
        {
            vm.ClearSelection();
            return;
        }

        switch (row.Kind)
        {
            case BranchRowKind.LocalHeader:
                vm.ToggleLocalSection();
                return;
            case BranchRowKind.RemotesHeader:
                vm.ToggleRemotesSection();
                return;
            case BranchRowKind.StashesHeader:
                vm.ToggleStashesSection();
                return;
            case BranchRowKind.RemoteHeader:
                if (row.RemoteName != null) vm.ToggleRemote(row.RemoteName);
                return;
            case BranchRowKind.Folder:
                if (row.FolderKey != null) vm.ToggleFolder(row.FolderKey);
                return;
            case BranchRowKind.LocalBranch:
                if (row.TipSha != null && row.FullPath != null)
                    vm.SelectLocalBranch(row.FullPath, row.TipSha);
                return;
            case BranchRowKind.RemoteBranch:
                if (row.TipSha != null && row.RemoteName != null && row.FullPath != null)
                    vm.SelectRemoteBranch(row.RemoteName, row.FullPath, row.TipSha);
                return;
            case BranchRowKind.Stash:
                if (row.TipSha != null && row.FullPath != null)
                    vm.SelectStash(row.FullPath, row.TipSha);
                return;
        }
    }

    private static void DispatchActivate(BranchesViewModel vm, BranchRow row)
    {
        switch (row.Kind)
        {
            case BranchRowKind.LocalBranch:
                if (row.FullPath != null) vm.ActivateLocalBranch(row.FullPath, row.IsHead);
                return;
            case BranchRowKind.RemoteBranch:
                if (row.RemoteName != null && row.FullPath != null)
                    vm.ActivateRemoteBranch(row.RemoteName, row.FullPath);
                return;
            case BranchRowKind.Stash:
                if (row.StashIndex is int idx && row.FullPath != null)
                    vm.ActivateStash(idx, row.FullPath, row.DisplayName);
                return;
        }
    }

    // Opens the context menu for the row under <point>, if any. Highlights that row for
    // the duration of the menu (so the user knows what the menu acts on even though the
    // pointer floats away from the row). Does not change selection or broadcast — pure
    // visual hint that clears when the menu closes.
    internal void OnRightClickAt(PointF point, Context context)
    {
        if (_vm == null) return;
        if (!Position.ContainsPoint(point)) return;
        var idx = HitTestRow(point);
        if (idx < 0 || idx >= _rows.Count) return;
        var row = _rows[idx];

        var items = BuildMenuItemsFor(_vm, row);
        if (items.Count == 0) return;

        _contextHighlightRowIndex = idx;
        var opened = RepoBarContextMenu.Show(context, point, items);
        if (opened == null)
        {
            _contextHighlightRowIndex = -1;
            return;
        }
        opened.Closed += () => _contextHighlightRowIndex = -1;
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItemsFor(BranchesViewModel vm, BranchRow row)
    {
        switch (row.Kind)
        {
            case BranchRowKind.LocalBranch when row.FullPath != null:
                return vm.BuildLocalBranchMenuItems(row.FullPath, row.IsHead);
            case BranchRowKind.RemoteBranch when row.RemoteName != null && row.FullPath != null:
                return vm.BuildRemoteBranchMenuItems(row.RemoteName, row.FullPath);
            default:
                return Array.Empty<RepoBarContextMenu.Item>();
        }
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

    // ---- row building ----
    //
    // Layout constants live in the view because the VM has no business picking pixel
    // offsets. Section headers ("Local" / "Remote" / "Stashes") and the per-row indent
    // math are render concerns that the VM produces no input for beyond the raw listing
    // and the open/closed flags.

    private const float IndentSection = 0f;
    private const float IndentRemoteHeader = 12f;
    private const float IndentLocalTreeBase = 16f;
    private const float IndentRemoteTreeBase = 28f;
    private const float IndentStashBase = 16f;
    private const float IndentLevel = 16f;

    private static IReadOnlyList<BranchRow> BuildRows(BranchListing? listing, BranchesUiState ui)
    {
        var rows = new List<BranchRow>();
        if (listing == null) return rows;

        rows.Add(new BranchRow(BranchRowKind.LocalHeader, "Local", IndentSection, ui.LocalOpen));
        if (ui.LocalOpen)
        {
            var localTree = BuildTree(listing.LocalBranches);
            EmitTreeRows(rows, localTree, ui, isRemote: false, remoteName: null, IndentLocalTreeBase, depth: 0);
        }

        rows.Add(new BranchRow(BranchRowKind.RemotesHeader, "Remote", IndentSection, ui.RemotesOpen));
        if (ui.RemotesOpen)
        {
            foreach (var rg in listing.Remotes)
            {
                var isOpen = ui.RemoteOpen.TryGetValue(rg.Name, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.RemoteHeader, rg.Name, IndentRemoteHeader, isOpen)
                {
                    RemoteName = rg.Name,
                });
                if (!isOpen) continue;
                var remoteTree = BuildTree(rg.Branches);
                EmitTreeRows(rows, remoteTree, ui, isRemote: true, rg.Name, IndentRemoteTreeBase, depth: 0);
            }
        }

        if (listing.Stashes.Count > 0)
        {
            rows.Add(new BranchRow(BranchRowKind.StashesHeader, "Stashes", IndentSection, ui.StashesOpen));
            if (ui.StashesOpen)
            {
                foreach (var s in listing.Stashes)
                {
                    var label = $"stash@{{{s.Index}}}";
                    rows.Add(new BranchRow(BranchRowKind.Stash, s.Subject, IndentStashBase, isOpen: false)
                    {
                        TipSha = s.Sha,
                        FullPath = label,
                        StashIndex = s.Index,
                    });
                }
            }
        }
        return rows;
    }

    private static void EmitTreeRows(List<BranchRow> rows, IReadOnlyList<TreeNode> nodes, BranchesUiState ui, bool isRemote, string? remoteName, float treeBase, int depth)
    {
        var indent = treeBase + depth * IndentLevel;
        foreach (var node in nodes)
        {
            if (node.Entry is { } entry)
            {
                rows.Add(new BranchRow(isRemote ? BranchRowKind.RemoteBranch : BranchRowKind.LocalBranch, node.Segment, indent, isOpen: false)
                {
                    TipSha = entry.TipSha,
                    IsHead = entry.IsHead,
                    RemoteName = remoteName,
                    FullPath = entry.Name,
                    AheadBy = entry.AheadBy,
                    BehindBy = entry.BehindBy,
                });
            }
            else
            {
                var key = MakeFolderKey(isRemote, remoteName, node.FullPath);
                var open = ui.FolderOpen.TryGetValue(key, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.Folder, node.Segment, indent, open)
                {
                    RemoteName = remoteName,
                    FullPath = node.FullPath,
                    FolderKey = key,
                });
                if (open) EmitTreeRows(rows, node.Children, ui, isRemote, remoteName, treeBase, depth + 1);
            }
        }
    }

    private static string MakeFolderKey(bool isRemote, string? remoteName, string path) =>
        isRemote ? $"remote:{remoteName}:{path}" : $"local:{path}";

    private static IReadOnlyList<TreeNode> BuildTree(IReadOnlyList<BranchEntry> branches)
    {
        var root = new TreeNode("", "");
        foreach (var b in branches)
        {
            var segments = b.Name.Split('/');
            var current = root;
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var isLeaf = i == segments.Length - 1;
                if (!current.ChildIndex.TryGetValue(seg, out var child))
                {
                    var path = i == 0 ? seg : current.FullPath + "/" + seg;
                    child = new TreeNode(seg, path);
                    current.ChildIndex[seg] = child;
                    current.Children.Add(child);
                }
                if (isLeaf) child.Entry = b;
                current = child;
            }
        }
        SortNode(root);
        return root.Children;
    }

    // Folders first, then leaves; alphabetical within each group. The leaf-of-the-same-path
    // case (a branch named "feature" alongside "feature/login") cannot occur in git, so we
    // don't try to handle a node that's simultaneously a folder and a branch.
    private static void SortNode(TreeNode node)
    {
        node.Children.Sort((a, b) =>
        {
            var aFolder = a.Entry == null;
            var bFolder = b.Entry == null;
            if (aFolder != bFolder) return aFolder ? -1 : 1;
            return string.Compare(a.Segment, b.Segment, StringComparison.OrdinalIgnoreCase);
        });
        foreach (var c in node.Children) SortNode(c);
    }

    private sealed class TreeNode
    {
        public TreeNode(string segment, string fullPath)
        {
            Segment = segment;
            FullPath = fullPath;
        }
        public string Segment { get; }
        public string FullPath { get; }
        public BranchEntry? Entry { get; set; }
        public Dictionary<string, TreeNode> ChildIndex { get; } = new();
        public List<TreeNode> Children { get; } = new();
    }
}
