using ZGF.Geometry;
using ZGF.Gui;

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
///
/// Scroll/hit-test/hover/double-click plumbing lives in <see cref="VirtualRowListView"/>;
/// row flattening lives in <see cref="BranchTreeBuilder"/>. This view owns the row
/// visuals (icons, ahead/behind badges, busy/head/worktree styling) and the dispatch
/// from row indices to <see cref="BranchesViewModel"/> calls.
/// </summary>
internal sealed class BranchesView : MultiChildView, IBind<BranchesViewModel>
{
    private const float RowHeight = 22f;
    private const float BaseIndent = 8f;
    private const float ChevronWidth = 14f;
    private const float ChevronGap = 2f;
    private const float ChevronColumn = ChevronWidth + ChevronGap;
    private const float IconGap = 4f;

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
    private readonly TextStyle _aheadNumStyle = TextStyles.Row(CommitsPalette.AheadColor);
    private readonly TextStyle _behindNumStyle = TextStyles.Row(CommitsPalette.BehindColor);
    private readonly TextStyle _aheadIconStyle = TextStyles.Icon(CommitsPalette.AheadColor);
    private readonly TextStyle _behindIconStyle = TextStyles.Icon(CommitsPalette.BehindColor);
    private readonly TextStyle _upstreamLinkedIconStyle = TextStyles.Icon(CommitsPalette.AheadColor);
    private readonly TextStyle _upstreamGoneIconStyle = TextStyles.Icon(CommitsPalette.BehindColor);
    private readonly TextStyle _folderIconStyle = TextStyles.Icon(DialogPalette.SectionHeaderText);
    private readonly TextStyle _branchIconStyle = TextStyles.Icon(CommitsPalette.RowText);
    private readonly TextStyle _branchIconActiveStyle = TextStyles.Icon(CommitsPalette.RowTextActive);
    private readonly TextStyle _branchIconLocalOnlyStyle = TextStyles.Icon(DialogPalette.RowTextMissing);

    private readonly VirtualRowListView _list;

    private IReadOnlyList<BranchRow> _rows = Array.Empty<BranchRow>();
    private BranchSelection? _selection;
    private string? _busyBranch;
    private string? _loadError;
    private bool _isLoading;
    private IReadOnlySet<string> _worktreeBranches = new HashSet<string>();

    private BranchListing? _listing;
    private BranchesUiState _ui = new();

    private BranchesViewModel? _vm;

    public BranchesView()
    {
        _list = new VirtualRowListView
        {
            RowHeight = RowHeight,
            ItemBuilder = DrawRowAt,
        };
        _list.RowClicked += OnRowClicked;
        _list.RowActivated += OnRowActivated;
        _list.RowContextRequested += OnRowContextRequested;

        AddChildToSelf(_list);
        _list.UseController(_ => new VirtualRowListController(_list));

        this.UseViewModel(this);
    }

    public void Bind(BranchesViewModel vm)
    {
        _vm = vm;
        vm.Listing.Subscribe(listing => { _listing = listing; RebuildRows(); });
        vm.Ui.Subscribe(ui => { _ui = ui; RebuildRows(); });
        vm.Selection.Subscribe(SetSelection);
        vm.BusyBranch.Subscribe(SetBusyBranch);
        vm.LoadError.Subscribe(SetLoadError);
        vm.IsLoading.Subscribe(SetIsLoading);
        vm.WorktreeBranches.Subscribe(set => _worktreeBranches = set);
    }

    private void RebuildRows()
    {
        _rows = BranchTreeBuilder.BuildRows(_listing, _ui);
        _list.ItemCount = _rows.Count;
        _list.NotifyItemsChanged();
    }

    private void SetSelection(BranchSelection? selection) => _selection = selection;
    private void SetBusyBranch(string? fullPath) => _busyBranch = fullPath;
    private void SetLoadError(string? error) => _loadError = error;
    private void SetIsLoading(bool isLoading) => _isLoading = isLoading;

    protected override void OnDrawSelf(ICanvas c)
    {
        var pos = Position;
        var z = GetDrawZIndex();

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
            return;
        }

        if (_rows.Count == 0 && _isLoading)
        {
            c.DrawText(new DrawTextInputs
            {
                Position = pos,
                Text = "Loading…",
                Style = _placeholderStyle,
                ZIndex = z + 1,
            });
        }
    }

    private void DrawRowAt(ICanvas c, RectF rowRect, int rowIndex, RowRenderState state, int z)
    {
        if (rowIndex < 0 || rowIndex >= _rows.Count) return;
        if (_loadError != null) return;

        var row = _rows[rowIndex];
        var isSelected = _selection.HasValue && _selection.Value.Matches(row);
        var rowBottom = rowRect.Bottom;
        var rightEdge = rowRect.Right - 14f;

        DrawRowBackground(c, rowRect, isSelected, state, z);

        var contentLeft = rowRect.Left + BaseIndent + row.Indent;
        contentLeft = DrawChevronOrReserveColumn(c, row, contentLeft, rowBottom, z + 1);

        if (IsTreeRow(row) && Context != null)
            contentLeft = DrawRowIcon(c, row, isSelected, contentLeft, rowBottom, z + 1);

        DrawRowNameAndBadge(c, row, isSelected, contentLeft, rightEdge, rowBottom, z + 1);
    }

    private void DrawRowBackground(ICanvas c, RectF rowRect, bool isSelected, RowRenderState state, int z)
    {
        var bg = isSelected
            ? CommitsPalette.RowHighlight
            : state.IsHovered || state.IsContextHighlighted 
                ? DialogPalette.RowHover
                : (uint?)null;
        
        if (bg == null) return;

        c.DrawRect(new DrawRectInputs
        {
            Position = rowRect,
            Style = new RectStyle { BackgroundColor = bg.Value },
            ZIndex = z,
        });
    }

    private static bool HasChevron(BranchRow row) =>
        row.Kind is BranchRowKind.LocalHeader
            or BranchRowKind.RemotesHeader
            or BranchRowKind.RemoteHeader
            or BranchRowKind.StashesHeader
            or BranchRowKind.Folder;

    private static bool IsTreeRow(BranchRow row) =>
        row.Kind is BranchRowKind.Folder
            or BranchRowKind.LocalBranch
            or BranchRowKind.RemoteBranch
            or BranchRowKind.Stash;

    private float DrawChevronOrReserveColumn(ICanvas c, BranchRow row, float contentLeft, float rowBottom, int z)
    {
        if (HasChevron(row))
        {
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(contentLeft, rowBottom, ChevronWidth, RowHeight),
                Text = row.IsOpen ? "▼" : "▶",
                Style = _chevronStyle,
                ZIndex = z,
            });
            return contentLeft + ChevronColumn;
        }

        // Branch rows reserve the chevron column so their icons sit in the same x
        // position as a sibling folder's icon.
        if (IsTreeRow(row))
            return contentLeft + ChevronColumn;

        return contentLeft;
    }

    private void DrawRowNameAndBadge(ICanvas c, BranchRow row, bool isSelected, float contentLeft, float rightEdge, float rowBottom, int z)
    {
        const float nameBadgeGap = 8f;
        var badgeWidth = (row.Kind == BranchRowKind.LocalBranch && Context != null)
            ? MeasureAheadBehindBadge(c, row)
            : 0f;

        var nameBudget = Math.Max(0f,
            rightEdge - contentLeft
            - (badgeWidth > 0 ? badgeWidth + nameBadgeGap : 0f));
        if (nameBudget <= 0f) return;

        var (text, style) = SelectNameTextAndStyle(row, isSelected);
        var rendered = TruncateToFit(text, style, nameBudget);

        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(contentLeft, rowBottom, nameBudget, RowHeight),
            Text = rendered,
            Style = style,
            ZIndex = z,
        });

        if (badgeWidth > 0)
        {
            var nameWidth = Context!.Canvas.MeasureTextWidth(rendered, style);
            DrawAheadBehindBadgeAt(c, row, contentLeft + nameWidth + nameBadgeGap, rowBottom, z);
        }
    }

    private (string text, TextStyle style) SelectNameTextAndStyle(BranchRow row, bool isSelected)
    {
        var isCheckedOutElsewhere = row.Kind == BranchRowKind.LocalBranch
            && row.FullPath != null
            && _worktreeBranches.Contains(row.FullPath);
        var isBusy = IsBusyRow(row);

        return row.Kind switch
        {
            BranchRowKind.LocalHeader or BranchRowKind.RemotesHeader or BranchRowKind.RemoteHeader or BranchRowKind.StashesHeader => (row.DisplayName, _headerTextStyle),
            BranchRowKind.LocalBranch when isBusy => (row.DisplayName, _branchTextBusyStyle),
            BranchRowKind.LocalBranch when isCheckedOutElsewhere => (row.DisplayName, _branchTextBusyStyle),
            BranchRowKind.LocalBranch when row.IsHead => (row.DisplayName, _headTextStyle),
            _ => (row.DisplayName, isSelected ? _branchTextSelectedStyle : _branchTextStyle),
        };
    }

    private float DrawRowIcon(ICanvas c, BranchRow row, bool isSelected, float left, float rowBottom, int z)
    {
        var (glyph, style) = SelectRowIcon(row, isSelected);
        var width = c.MeasureTextWidth(glyph, style);

        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(left, rowBottom, width, RowHeight),
            Text = glyph,
            Style = style,
            ZIndex = z,
        });
        return left + width + IconGap;
    }

    private (string glyph, TextStyle style) SelectRowIcon(BranchRow row, bool isSelected)
    {
        return row.Kind switch
        {
            BranchRowKind.Folder => (
                row.IsOpen ? LucideIcons.FolderOpen : LucideIcons.Folder,
                _folderIconStyle),
            BranchRowKind.Stash => (
                LucideIcons.Stash,
                isSelected ? _branchIconActiveStyle : _branchIconStyle),
            _ => SelectBranchIcon(row, isSelected),
        };
    }

    private (string glyph, TextStyle style) SelectBranchIcon(BranchRow row, bool isSelected)
    {
        if (IsBusyRow(row))
            return (LucideIcons.Branch, _branchIconBusyStyle);

        if (row.Kind == BranchRowKind.LocalBranch)
        {
            return row.UpstreamState switch
            {
                BranchUpstreamState.Tracked => (LucideIcons.Branch, _upstreamLinkedIconStyle),
                BranchUpstreamState.Gone => (LucideIcons.CloudOff, _upstreamGoneIconStyle),
                _ => (LucideIcons.Branch, _branchIconLocalOnlyStyle),
            };
        }

        var style = (row.IsHead || isSelected) ? _branchIconActiveStyle : _branchIconStyle;
        return (LucideIcons.Branch, style);
    }

    private const float BadgeGap = 8f;
    private const float BadgeNumIconGap = 0f;

    private float MeasureAheadBehindBadge(ICanvas canvas, BranchRow row)
    {
        var ahead = row.AheadBy.GetValueOrDefault();
        var behind = row.BehindBy.GetValueOrDefault();
        if (ahead == 0 && behind == 0) return 0f;

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

    private void OnRowClicked(int rowIndex)
    {
        if (_vm == null) return;
        var row = (rowIndex >= 0 && rowIndex < _rows.Count) ? _rows[rowIndex] : null;
        DispatchClick(_vm, row);
    }

    private void OnRowActivated(int rowIndex)
    {
        if (_vm == null) return;
        if (rowIndex < 0 || rowIndex >= _rows.Count) return;
        DispatchActivate(_vm, _rows[rowIndex]);
    }

    private void OnRowContextRequested(int rowIndex, PointF point)
    {
        if (_vm == null) return;
        if (rowIndex < 0 || rowIndex >= _rows.Count) return;
        var row = _rows[rowIndex];

        var items = BuildMenuItemsFor(_vm, row);
        if (items.Count == 0) return;
        if (Context == null) return;

        _list.SetContextHighlight(rowIndex);
        var opened = RepoBarContextMenu.Show(Context, point, items);
        if (opened == null)
        {
            _list.SetContextHighlight(null);
            return;
        }
        opened.Closed += () => _list.SetContextHighlight(null);
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
}
