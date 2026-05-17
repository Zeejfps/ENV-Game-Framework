using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Sidebar listing local branches and remote branches (grouped per remote). Click a branch
/// row to scroll/select its tip commit in the history view; click a section/remote header
/// to toggle collapse. Collapse state is persisted per-repo via IRepoRegistry. Currently
/// read-only — no checkout, rename, delete, push, pull from this view.
/// </summary>
public sealed class BranchesView : MultiChildView
{
    private const float RowHeight = 22f;
    private const float BaseIndent = 8f;
    private const float ChevronWidth = 14f;
    private const float ChevronGap = 2f;
    private const float ScrollWheelStep = 60f;

    private const float IndentSection = 0f;       // LOCAL / REMOTES
    private const float IndentRemoteHeader = 12f; // origin (under REMOTES)
    private const float IndentLocalBranch = 16f;  // branch row under LOCAL
    private const float IndentRemoteBranch = 28f; // branch row under origin

    private IMessageBus? _bus;
    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private State<MainViewMode>? _mode;
    private IDisposable? _activeSubscription;
    private IDisposable? _commitCreatedSubscription;
    private IDisposable? _commitSelectedSubscription;

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
            foreach (var b in listing.LocalBranches)
            {
                _rows.Add(new Row(RowKind.LocalBranch, b.Name, IndentLocalBranch, IsOpen: false)
                {
                    TipSha = b.TipSha,
                    IsHead = b.IsHead,
                    AheadBy = b.AheadBy,
                    BehindBy = b.BehindBy,
                });
            }
        }

        _rows.Add(new Row(RowKind.RemotesHeader, "Remotes", IndentSection, _ui.RemotesOpen));
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
                foreach (var b in rg.Branches)
                {
                    _rows.Add(new Row(RowKind.RemoteBranch, b.Name, IndentRemoteBranch, IsOpen: false)
                    {
                        TipSha = b.TipSha,
                        RemoteName = rg.Name,
                    });
                }
            }
        }

        ClampScroll();
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
            || row.Kind == RowKind.RemoteHeader;
        if (hasChevron)
        {
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(contentLeft, rowBottom, ChevronWidth, RowHeight),
                Text = row.IsOpen ? "▼" : "▶",
                Style = _chevronStyle,
                ZIndex = z + 1,
            });
            contentLeft += ChevronWidth + ChevronGap;
        }

        // Ahead/behind badge eats from the right edge before the branch name is truncated.
        if (row.Kind == RowKind.LocalBranch && Context != null)
            rightEdge = DrawAheadBehindBadge(c, row, rowBottom, rightEdge, z + 1);

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
            case RowKind.LocalBranch:
                if (row.TipSha != null)
                {
                    _selection = new BranchSelection(IsRemote: false, RemoteName: null, Name: row.DisplayName, TipSha: row.TipSha);
                    SwitchToHistory();
                    _bus?.Broadcast(new CommitSelectedMessage(_activeRepoId, row.TipSha));
                }
                return;
            case RowKind.RemoteBranch:
                if (row.TipSha != null && row.RemoteName != null)
                {
                    _selection = new BranchSelection(IsRemote: true, RemoteName: row.RemoteName, Name: row.DisplayName, TipSha: row.TipSha);
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

    private readonly record struct BranchSelection(bool IsRemote, string? RemoteName, string Name, string TipSha)
    {
        public bool Matches(Row row) => row.Kind switch
        {
            RowKind.LocalBranch => !IsRemote && row.DisplayName == Name,
            RowKind.RemoteBranch => IsRemote && row.RemoteName == RemoteName && row.DisplayName == Name,
            _ => false,
        };
    }

    private enum RowKind { LocalHeader, RemotesHeader, RemoteHeader, LocalBranch, RemoteBranch }

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
        public int? AheadBy { get; init; }
        public int? BehindBy { get; init; }
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
