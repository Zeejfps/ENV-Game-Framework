using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Sidebar listing local branches and remote branches (grouped per remote) as a tree —
/// branch names containing "/" are split into folder nodes (e.g. "feature/login" lives
/// inside a "feature" folder). Click a branch row to scroll/select its tip commit in the
/// history view; click a section/remote/folder row to toggle collapse. Collapse state is
/// persisted per-repo via IRepoRegistry. Currently read-only — no checkout, rename,
/// delete, push, pull from this view.
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

    private const float IndentSection = 0f;          // LOCAL / REMOTES
    private const float IndentRemoteHeader = 12f;    // origin (under REMOTES)
    private const float IndentLocalTreeBase = 16f;   // depth-0 row under LOCAL
    private const float IndentRemoteTreeBase = 28f;  // depth-0 row under a remote header
    private const float IndentLevel = 16f;           // per-depth step within the tree

    private IMessageBus? _bus;
    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private State<MainViewMode>? _mode;
    private IDisposable? _activeSubscription;
    private IDisposable? _commitCreatedSubscription;
    private IDisposable? _commitSelectedSubscription;
    private IDisposable? _refsChangedSubscription;

    private Guid _activeRepoId;
    private int _loadGeneration;
    private BranchListing? _listing;
    private string? _loadError;
    private BranchesUiState _ui = new();
    private BranchSelection? _selection;

    private readonly List<Row> _rows = new();
    private float _scrollY;
    private int _hoveredRowIndex = -1;

    private readonly TextStyle _branchTextStyle = new()
    {
        TextColor = CommitsPalette.RowText,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _branchTextSelectedStyle = new()
    {
        TextColor = CommitsPalette.RowTextActive,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _headTextStyle = new()
    {
        TextColor = CommitsPalette.RowTextActive,
        FontWeight = FontWeight.Bold,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _headerTextStyle = new()
    {
        TextColor = DialogPalette.SectionHeaderText,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _chevronStyle = new()
    {
        TextColor = DialogPalette.SectionHeaderText,
        FontSize = 8f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Center,
    };
    private readonly TextStyle _placeholderStyle = new()
    {
        TextColor = CommitsPalette.Placeholder,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Center,
    };
    // Ahead = "need to push", behind = "need to pull". Greenish + amber for at-a-glance.
    // Number uses the default font; icon uses the Lucide glyphs the toolbar push/pull
    // buttons use so the visual vocabulary stays consistent.
    private const uint AheadColor = 0xFF9DD17B;
    private const uint BehindColor = 0xFFE6A85C;
    private readonly TextStyle _aheadNumStyle = new()
    {
        TextColor = AheadColor,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _behindNumStyle = new()
    {
        TextColor = BehindColor,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _aheadIconStyle = new()
    {
        TextColor = AheadColor,
        FontFamily = LucideIcons.FontFamily,
        FontSize = 14f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _behindIconStyle = new()
    {
        TextColor = BehindColor,
        FontFamily = LucideIcons.FontFamily,
        FontSize = 14f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _folderIconStyle = new()
    {
        TextColor = DialogPalette.SectionHeaderText,
        FontFamily = LucideIcons.FontFamily,
        FontSize = 14f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _branchIconStyle = new()
    {
        TextColor = CommitsPalette.RowText,
        FontFamily = LucideIcons.FontFamily,
        FontSize = 14f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _branchIconActiveStyle = new()
    {
        TextColor = CommitsPalette.RowTextActive,
        FontFamily = LucideIcons.FontFamily,
        FontSize = 14f,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };

    public BranchesView()
    {
        Behaviors.Add(new BranchesViewController(this));
    }

    protected override void OnAttachedToContext(Context context)
    {
        _bus = context.Get<IMessageBus>();
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _mode = context.Get<State<MainViewMode>>();

        if (_registry != null)
            _activeSubscription = _registry.Active.Subscribe(_ => OnActiveRepoChanged());

        _commitCreatedSubscription = _bus?.SubscribeScoped<CommitCreatedMessage>(OnCommitCreated);
        _commitSelectedSubscription = _bus?.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected);
        _refsChangedSubscription = _bus?.SubscribeScoped<RefsChangedMessage>(OnRefsChanged);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _loadGeneration++;
        _activeSubscription?.Dispose();
        _activeSubscription = null;
        _commitCreatedSubscription?.Dispose();
        _commitCreatedSubscription = null;
        _commitSelectedSubscription?.Dispose();
        _commitSelectedSubscription = null;
        _refsChangedSubscription?.Dispose();
        _refsChangedSubscription = null;
        _bus = null;
        _registry = null;
        _gitService = null;
        _dispatcher = null;
        _mode = null;
    }

    private void OnActiveRepoChanged()
    {
        var active = _registry?.Active.Value;
        _activeRepoId = active?.Id ?? Guid.Empty;
        _selection = null;
        _scrollY = 0f;
        _hoveredRowIndex = -1;

        if (active == null)
        {
            _listing = null;
            _loadError = null;
            _ui = new BranchesUiState();
            RebuildRows();
            return;
        }

        _ui = _registry?.GetBranchesUi(active.Id) ?? new BranchesUiState();
        StartLoad(active);
    }

    private void OnCommitCreated(CommitCreatedMessage msg)
    {
        var active = _registry?.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoad(active);
    }

    private void OnRefsChanged(RefsChangedMessage msg)
    {
        var active = _registry?.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoad(active);
    }

    // Clear the sidebar selection when commit selection moves to anything other than the
    // currently-selected branch's tip. Self-broadcasts (same SHA) short-circuit.
    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (msg.RepoId != _activeRepoId) return;
        if (_selection.HasValue && msg.Sha == _selection.Value.TipSha) return;
        _selection = null;
    }

    private void StartLoad(Repo repo)
    {
        var gitService = _gitService;
        var dispatcher = _dispatcher;
        if (gitService == null) return;

        _loadGeneration++;
        var gen = _loadGeneration;

        Task.Run(() =>
        {
            BranchListing listing;
            try
            {
                listing = gitService.GetBranches(repo);
            }
            catch (Exception ex)
            {
                listing = new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), ex.Message);
            }

            dispatcher?.Post(() =>
            {
                if (gen != _loadGeneration) return;
                if (repo.Id != _activeRepoId) return;
                _listing = listing.ErrorMessage == null ? listing : null;
                _loadError = listing.ErrorMessage;
                RebuildRows();
            });
        });
    }

    private void RebuildRows()
    {
        _rows.Clear();
        var listing = _listing;
        if (listing == null) return;

        _rows.Add(new Row(RowKind.LocalHeader, "Local", IndentSection, _ui.LocalOpen));
        if (_ui.LocalOpen)
        {
            var localTree = BuildTree(listing.LocalBranches);
            EmitTreeRows(localTree, isRemote: false, remoteName: null, IndentLocalTreeBase, depth: 0);
        }

        _rows.Add(new Row(RowKind.RemotesHeader, "Remote", IndentSection, _ui.RemotesOpen));
        if (_ui.RemotesOpen)
        {
            foreach (var rg in listing.Remotes)
            {
                var isOpen = _ui.RemoteOpen.TryGetValue(rg.Name, out var v) ? v : true;
                _rows.Add(new Row(RowKind.RemoteHeader, rg.Name, IndentRemoteHeader, isOpen)
                {
                    RemoteName = rg.Name,
                });
                if (!isOpen) continue;
                var remoteTree = BuildTree(rg.Branches);
                EmitTreeRows(remoteTree, isRemote: true, rg.Name, IndentRemoteTreeBase, depth: 0);
            }
        }

        ClampScroll();
    }

    private void EmitTreeRows(IReadOnlyList<TreeNode> nodes, bool isRemote, string? remoteName, float treeBase, int depth)
    {
        var indent = treeBase + depth * IndentLevel;
        foreach (var node in nodes)
        {
            if (node.Entry is { } entry)
            {
                _rows.Add(new Row(isRemote ? RowKind.RemoteBranch : RowKind.LocalBranch, node.Segment, indent, IsOpen: false)
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
                var open = _ui.FolderOpen.TryGetValue(key, out var v) ? v : true;
                _rows.Add(new Row(RowKind.Folder, node.Segment, indent, open)
                {
                    RemoteName = remoteName,
                    FullPath = node.FullPath,
                    FolderKey = key,
                });
                if (open) EmitTreeRows(node.Children, isRemote, remoteName, treeBase, depth + 1);
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

    private void ClampScroll()
    {
        var bodyHeight = Position.Height;
        if (bodyHeight <= 0) return;
        var contentHeight = _rows.Count * RowHeight;
        var maxScroll = Math.Max(0f, contentHeight - bodyHeight);
        _scrollY = Math.Clamp(_scrollY, 0f, maxScroll);
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

        if (_listing == null || _rows.Count == 0)
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

    private void DrawRow(ICanvas c, RectF pos, Row row, int rowIndex, float rowBottom, int z)
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

        var hasChevron = row.Kind == RowKind.LocalHeader
            || row.Kind == RowKind.RemotesHeader
            || row.Kind == RowKind.RemoteHeader
            || row.Kind == RowKind.Folder;
        var isTreeRow = row.Kind == RowKind.Folder
            || row.Kind == RowKind.LocalBranch
            || row.Kind == RowKind.RemoteBranch;

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
        if (row.Kind == RowKind.LocalBranch && Context != null)
            rightEdge = DrawAheadBehindBadge(c, row, rowBottom, rightEdge, z + 1);

        if (isTreeRow && Context != null)
            contentLeft = DrawRowIcon(c, row, isSelected, contentLeft, rowBottom, z + 1);

        var textWidth = Math.Max(0f, rightEdge - contentLeft);
        if (textWidth <= 0f) return;

        var (text, style) = row.Kind switch
        {
            RowKind.LocalHeader or RowKind.RemotesHeader or RowKind.RemoteHeader => (row.DisplayName, _headerTextStyle),
            RowKind.LocalBranch when row.IsHead => (row.DisplayName, _headTextStyle),
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

    private float DrawRowIcon(ICanvas c, Row row, bool isSelected, float left, float rowBottom, int z)
    {
        string glyph;
        TextStyle style;
        if (row.Kind == RowKind.Folder)
        {
            glyph = row.IsOpen ? LucideIcons.FolderOpen : LucideIcons.Folder;
            style = _folderIconStyle;
        }
        else
        {
            glyph = LucideIcons.Branch;
            style = (row.IsHead || isSelected) ? _branchIconActiveStyle : _branchIconStyle;
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
    private float DrawAheadBehindBadge(ICanvas c, Row row, float rowBottom, float rightEdge, int z)
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

    private string TruncateToFit(string text, TextStyle style, float available)
    {
        if (Context == null || string.IsNullOrEmpty(text)) return text;
        var full = Context.Canvas.MeasureTextWidth(text, style);
        if (full <= available) return text;

        const string ellipsis = "…";
        var ellipsisWidth = Context.Canvas.MeasureTextWidth(ellipsis, style);
        if (ellipsisWidth > available) return ellipsis;

        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (Context.Canvas.MeasureTextWidth(text.AsSpan(0, mid), style) + ellipsisWidth <= available)
                lo = mid;
            else
                hi = mid - 1;
        }
        return text[..lo] + ellipsis;
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
        if (idx < 0 || idx >= _rows.Count)
        {
            ClearSelectionAndBroadcast();
            return;
        }

        var row = _rows[idx];
        switch (row.Kind)
        {
            case RowKind.LocalHeader:
                _ui.LocalOpen = !_ui.LocalOpen;
                PersistUi();
                RebuildRows();
                return;
            case RowKind.RemotesHeader:
                _ui.RemotesOpen = !_ui.RemotesOpen;
                PersistUi();
                RebuildRows();
                return;
            case RowKind.RemoteHeader:
                if (row.RemoteName != null)
                {
                    var current = _ui.RemoteOpen.TryGetValue(row.RemoteName, out var v) ? v : true;
                    _ui.RemoteOpen[row.RemoteName] = !current;
                    PersistUi();
                    RebuildRows();
                }
                return;
            case RowKind.Folder:
                if (row.FolderKey != null)
                {
                    var current = _ui.FolderOpen.TryGetValue(row.FolderKey, out var v) ? v : true;
                    _ui.FolderOpen[row.FolderKey] = !current;
                    PersistUi();
                    RebuildRows();
                }
                return;
            case RowKind.LocalBranch:
                if (row.TipSha != null && row.FullPath != null)
                {
                    _selection = new BranchSelection(IsRemote: false, RemoteName: null, FullPath: row.FullPath, TipSha: row.TipSha);
                    SwitchToHistory();
                    _bus?.Broadcast(new CommitSelectedMessage(_activeRepoId, row.TipSha));
                }
                return;
            case RowKind.RemoteBranch:
                if (row.TipSha != null && row.RemoteName != null && row.FullPath != null)
                {
                    _selection = new BranchSelection(IsRemote: true, RemoteName: row.RemoteName, FullPath: row.FullPath, TipSha: row.TipSha);
                    SwitchToHistory();
                    _bus?.Broadcast(new CommitSelectedMessage(_activeRepoId, row.TipSha));
                }
                return;
        }
    }

    private void SwitchToHistory()
    {
        if (_mode == null) return;
        if (_mode.Value == MainViewMode.History) return;
        _mode.Value = MainViewMode.History;
    }

    private void ClearSelectionAndBroadcast()
    {
        if (_selection == null) return;
        _selection = null;
        _bus?.Broadcast(new CommitSelectedMessage(_activeRepoId, null));
    }

    private bool IsPointInside(PointF point)
    {
        var pos = Position;
        return point.X >= pos.Left && point.X <= pos.Right
            && point.Y >= pos.Bottom && point.Y <= pos.Top;
    }

    private void PersistUi()
    {
        if (_registry == null || _activeRepoId == Guid.Empty) return;
        _registry.SetBranchesUi(_activeRepoId, _ui);
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

    private readonly record struct BranchSelection(bool IsRemote, string? RemoteName, string FullPath, string TipSha)
    {
        public bool Matches(Row row) => row.Kind switch
        {
            RowKind.LocalBranch => !IsRemote && row.FullPath == FullPath,
            RowKind.RemoteBranch => IsRemote && row.RemoteName == RemoteName && row.FullPath == FullPath,
            _ => false,
        };
    }

    private enum RowKind { LocalHeader, RemotesHeader, RemoteHeader, Folder, LocalBranch, RemoteBranch }

    private sealed class Row
    {
        public Row(RowKind kind, string displayName, float indent, bool IsOpen)
        {
            Kind = kind;
            DisplayName = displayName;
            Indent = indent;
            this.IsOpen = IsOpen;
        }
        public RowKind Kind { get; }
        public string DisplayName { get; }
        public float Indent { get; }
        public bool IsOpen { get; }
        public string? TipSha { get; init; }
        public bool IsHead { get; init; }
        public string? RemoteName { get; init; }
        public string? FullPath { get; init; }
        public string? FolderKey { get; init; }
        public int? AheadBy { get; init; }
        public int? BehindBy { get; init; }
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

internal sealed class BranchesViewController : KeyboardMouseController
{
    private readonly BranchesView _view;

    public BranchesViewController(BranchesView view)
    {
        _view = view;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.OnWheel(e.DeltaY);
        e.Consume();
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        _view.SetHover(e.Mouse.Point);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _view.ClearHover();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button != MouseButton.Left) return;
        if (e.State != InputState.Pressed) return;
        _view.OnClickAt(e.Mouse.Point);
        e.Consume();
    }
}
