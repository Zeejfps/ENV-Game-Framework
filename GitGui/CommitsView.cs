using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal static class CommitsPalette
{
    public const uint Background = 0xFF1E1F22;
    public const uint Border = 0xFF313338;
    public const uint HeaderBg = 0xFF2A2C30;
    public const uint HeaderText = 0xFF96989D;
    public const uint RowText = 0xFFB5B9C0;
    public const uint RowTextDim = 0xFF7A7C81;
    public const uint RowHighlight = 0xFF404C8C;
    public const uint RowTextActive = 0xFFFFFFFF;
    public const uint Placeholder = 0xFF96989D;

    public const uint ScrollTrackBg = 0xFF26272B;
    public const uint ScrollTrackBorder = 0xFF313338;
    public const uint ScrollThumbBg = 0xFF4A4D52;
    public const uint ScrollThumbHoverBg = 0xFF6A6D72;
    public const uint ScrollThumbBorder = 0xFF2A2C30;

    public const uint DividerHoverBg = 0xFF4A5680;
    public const uint DividerHoverLine = 0xFF7A8DC8;

    public const uint WarningBg = 0xFF3D2E14;
    public const uint WarningBorder = 0xFFB89050;
    public const uint WarningText = 0xFFE9C77A;

    public const uint BadgeLocalBg = 0xFF2F4A6B;
    public const uint BadgeRemoteBg = 0xFF4A2F6B;
    public const uint BadgeHeadBg = 0xFF6B4A2F;
    public const uint BadgeText = 0xFFE6E6E6;

    public static readonly uint[] LanePalette =
    {
        0xFF5865F2,
        0xFFEB459E,
        0xFF57F287,
        0xFFFEE75C,
        0xFFED4245,
        0xFF9B59B6,
        0xFFE67E22,
        0xFF1ABC9C,
    };

    public static uint LaneColor(int lane) => LanePalette[((lane % LanePalette.Length) + LanePalette.Length) % LanePalette.Length];
}

internal enum CommitsLoadState
{
    NoRepo,
    Loading,
    Loaded,
    Error,
}

public sealed class CommitsView : MultiChildView
{
    private const float HeaderHeight = 28f;
    private const float RowHeight = 26f;
    private const float LaneWidth = 16f;
    private const int MaxRenderedLanes = 12;
    private const float DotRadius = 5f;
    private const float EdgeThickness = 2f;
    private const float GraphColumnPaddingLeft = 12f;
    private const float GraphColumnPaddingRight = 8f;
    private const float ColumnGap = 12f;
    private const float ScrollWheelStep = 60f;

    private const float SummaryColumnWidth = 0f;
    private const float DefaultAuthorColumnWidth = 140f;
    private const float DefaultHashColumnWidth = 80f;
    private const float DefaultDateColumnWidth = 110f;
    private const float MinColumnWidth = 40f;
    private const float MaxColumnWidth = 600f;
    private const float DividerThickness = 1f;
    private const float DividerHitWidth = 6f;
    private const float BadgePaddingX = 6f;
    private const float BadgeHeight = 16f;
    private const float BadgeGap = 4f;

    private float _authorColumnWidth = DefaultAuthorColumnWidth;
    private float _hashColumnWidth = DefaultHashColumnWidth;
    private float _dateColumnWidth = DefaultDateColumnWidth;
    private DividerKind _hoveredDivider = DividerKind.None;

    private IMessageBus? _bus;
    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IDisposable? _activeSubscription;
    private Action<CommitCreatedMessage>? _commitCreatedHandler;

    private CommitsLoadState _state = CommitsLoadState.NoRepo;
    private CommitSnapshot? _snapshot;
    private int _loadGeneration;
    private Guid _loadingRepoId;

    private float _scrollY;
    private float _lastNormalizedScroll;
    private float _lastScale = 1f;
    private string? _selectedSha;

    public event Action<float>? ScrollPositionChanged;
    public event Action<float>? ScaleChanged;

    private readonly TextStyle _rowTextStyle = new()
    {
        TextColor = CommitsPalette.RowText,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _rowTextActiveStyle = new()
    {
        TextColor = CommitsPalette.RowTextActive,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _headerTextStyle = new()
    {
        TextColor = CommitsPalette.HeaderText,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _placeholderStyle = new()
    {
        TextColor = CommitsPalette.Placeholder,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Center,
    };
    private readonly TextStyle _badgeTextStyle = new()
    {
        TextColor = CommitsPalette.BadgeText,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _hashTextStyle = new()
    {
        TextColor = CommitsPalette.RowTextDim,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _hashTextActiveStyle = new()
    {
        TextColor = CommitsPalette.RowTextActive,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };

    public CommitsView()
    {
        Behaviors.Add(new CommitsViewController(this));
    }

    protected override void OnAttachedToContext(Context context)
    {
        _bus = context.Get<IMessageBus>();
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        if (_registry != null)
        {
            _activeSubscription = _registry.Active.Subscribe(_ => StartLoadForActiveRepo());
        }
        if (_bus != null)
        {
            _commitCreatedHandler = OnCommitCreated;
            _bus.Subscribe(_commitCreatedHandler);
        }
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _loadGeneration++;
        _activeSubscription?.Dispose();
        _activeSubscription = null;
        if (_bus != null && _commitCreatedHandler != null)
            _bus.Unsubscribe(_commitCreatedHandler);
        _commitCreatedHandler = null;
        _bus = null;
        _registry = null;
        _gitService = null;
        _dispatcher = null;
    }

    private void OnCommitCreated(CommitCreatedMessage msg)
    {
        var active = _registry?.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        // Drop the cached snapshot so StartLoadForActiveRepo doesn't short-circuit.
        _snapshot = null;
        StartLoadForActiveRepo();
    }

    private void StartLoadForActiveRepo()
    {
        if (_registry == null || _gitService == null) return;
        var active = _registry.Active.Value;

        // Preserve scroll/selection across detach/reattach (tab round-trip): if the snapshot
        // we already have matches the active repo and loaded cleanly, skip the reload.
        if (active != null
            && _state == CommitsLoadState.Loaded
            && _snapshot?.RepoId == active.Id)
        {
            _loadingRepoId = active.Id;
            return;
        }

        _loadGeneration++;
        var gen = _loadGeneration;

        if (active == null)
        {
            _state = CommitsLoadState.NoRepo;
            _snapshot = null;
            _scrollY = 0f;
            ClearSelection();
            NotifyScrollChanged();
            return;
        }

        _state = CommitsLoadState.Loading;
        _snapshot = null;
        _scrollY = 0f;
        ClearSelection();
        _loadingRepoId = active.Id;
        NotifyScrollChanged();

        var repo = active;
        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            CommitSnapshot snap;
            try
            {
                snap = service.Load(repo, 3000);
            }
            catch (Exception ex)
            {
                snap = new CommitSnapshot(repo.Id, repo.Path, Array.Empty<CommitNode>(), 0, false, ex.Message);
            }

            dispatcher?.Post(() =>
            {
                if (gen != _loadGeneration) return;
                ApplyLoadedSnapshot(snap);
            });
        });
    }

    public bool Truncated => _snapshot?.Truncated == true;
    public event Action<bool>? TruncatedChanged;

    private void ApplyLoadedSnapshot(CommitSnapshot snap)
    {
        if (snap.RepoId != _loadingRepoId) return;
        var wasTruncated = Truncated;
        _snapshot = snap;
        _state = snap.ErrorMessage != null ? CommitsLoadState.Error : CommitsLoadState.Loaded;
        _scrollY = 0f;
        NotifyScrollChanged();
        if (wasTruncated != Truncated)
            TruncatedChanged?.Invoke(Truncated);
        _bus?.Broadcast(new CommitsLoadedMessage(snap.RepoId));
    }

    public float Scale
    {
        get
        {
            var snap = _snapshot;
            if (snap == null || snap.Commits.Count == 0) return 1f;
            var bodyHeight = Position.Height - HeaderHeight;
            if (bodyHeight <= 0) return 1f;
            var contentHeight = snap.Commits.Count * RowHeight;
            if (contentHeight <= bodyHeight) return 1f;
            return bodyHeight / contentHeight;
        }
    }

    public void SetNormalizedScrollPosition(float normalized)
    {
        var snap = _snapshot;
        if (snap == null) return;
        var bodyHeight = Position.Height - HeaderHeight;
        var contentHeight = snap.Commits.Count * RowHeight;
        var maxScroll = Math.Max(0f, contentHeight - bodyHeight);
        var newScroll = maxScroll * Math.Clamp(normalized, 0f, 1f);
        if (Math.Abs(newScroll - _scrollY) < 0.0001f) return;
        _scrollY = newScroll;
        NotifyScrollChanged();
    }

    private void NotifyScrollChanged()
    {
        var snap = _snapshot;
        float normalized = 0f;
        float scale = 1f;
        if (snap != null && snap.Commits.Count > 0)
        {
            var bodyHeight = Position.Height - HeaderHeight;
            var contentHeight = snap.Commits.Count * RowHeight;
            if (bodyHeight > 0 && contentHeight > bodyHeight)
            {
                scale = bodyHeight / contentHeight;
                var maxScroll = contentHeight - bodyHeight;
                normalized = Math.Clamp(_scrollY / maxScroll, 0f, 1f);
            }
        }

        if (Math.Abs(scale - _lastScale) > 0.0001f)
        {
            _lastScale = scale;
            ScaleChanged?.Invoke(scale);
        }

        if (Math.Abs(normalized - _lastNormalizedScroll) > 0.0001f)
        {
            _lastNormalizedScroll = normalized;
            ScrollPositionChanged?.Invoke(normalized);
        }
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

        DrawHeader(c, pos, z + 1);

        var bodyTop = pos.Top - HeaderHeight;
        var bodyRect = new RectF(pos.Left, pos.Bottom, pos.Width, bodyTop - pos.Bottom);

        switch (_state)
        {
            case CommitsLoadState.NoRepo:
                DrawPlaceholder(c, ComputeCommitsColumnRect(bodyRect), "Select a repository to view its history.", z + 2);
                break;
            case CommitsLoadState.Loading:
                DrawPlaceholder(c, ComputeCommitsColumnRect(bodyRect), "Loading…", z + 2);
                break;
            case CommitsLoadState.Error:
                DrawPlaceholder(c, ComputeCommitsColumnRect(bodyRect), _snapshot?.ErrorMessage ?? "Error.", z + 2);
                break;
            case CommitsLoadState.Loaded:
                DrawCommits(c, bodyRect, z + 2);
                break;
        }

        var dateXAll = pos.Right - _dateColumnWidth - ColumnGap;
        var hashXAll = dateXAll - _hashColumnWidth - ColumnGap;
        var authorXAll = hashXAll - _authorColumnWidth - ColumnGap;
        DrawColumnDivider(c, authorXAll - ColumnGap, pos.Bottom, pos.Height, DividerKind.Author, z + 100);
        DrawColumnDivider(c, hashXAll - ColumnGap, pos.Bottom, pos.Height, DividerKind.Hash, z + 100);
        DrawColumnDivider(c, dateXAll - ColumnGap, pos.Bottom, pos.Height, DividerKind.Date, z + 100);

        c.PopClip();
    }

    private void DrawHeader(ICanvas c, RectF pos, int z)
    {
        var headerRect = new RectF(pos.Left, pos.Top - HeaderHeight, pos.Width, HeaderHeight);
        c.DrawRect(new DrawRectInputs
        {
            Position = headerRect,
            Style = new RectStyle
            {
                BackgroundColor = CommitsPalette.HeaderBg,
                BorderColor = new BorderColorStyle { Bottom = CommitsPalette.Border },
                BorderSize = new BorderSizeStyle { Bottom = 1 },
            },
            ZIndex = z,
        });

        var graphWidth = ComputeGraphColumnWidth();
        var dateX = pos.Right - _dateColumnWidth - ColumnGap;
        var hashX = dateX - _hashColumnWidth - ColumnGap;
        var authorX = hashX - _authorColumnWidth - ColumnGap;

        DrawHeaderText(c, "Commit", pos.Left + GraphColumnPaddingLeft, pos.Top - HeaderHeight, graphWidth, z + 1);
        DrawHeaderText(c, "Author", authorX, pos.Top - HeaderHeight, _authorColumnWidth, z + 1);
        DrawHeaderText(c, "Hash", hashX, pos.Top - HeaderHeight, _hashColumnWidth, z + 1);
        DrawHeaderText(c, "Date", dateX, pos.Top - HeaderHeight, _dateColumnWidth, z + 1);
    }

    private static void DrawColumnOverlay(ICanvas c, float left, float bottom, float width, uint color, int z)
    {
        if (width <= 0) return;
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(left, bottom, width, RowHeight),
            Style = new RectStyle { BackgroundColor = color },
            ZIndex = z,
        });
    }

    private void DrawColumnDivider(ICanvas c, float centerX, float bottom, float height, DividerKind kind, int z)
    {
        var hovered = _hoveredDivider == kind;
        if (hovered)
        {
            c.DrawRect(new DrawRectInputs
            {
                Position = new RectF(centerX - DividerHitWidth * 0.5f, bottom, DividerHitWidth, height),
                Style = new RectStyle { BackgroundColor = CommitsPalette.DividerHoverBg },
                ZIndex = z,
            });
        }
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(centerX - DividerThickness * 0.5f, bottom, DividerThickness, height),
            Style = new RectStyle { BackgroundColor = hovered ? CommitsPalette.DividerHoverLine : CommitsPalette.Border },
            ZIndex = z + 1,
        });
    }

    private void DrawHeaderText(ICanvas c, string text, float left, float bottom, float width, int z)
    {
        if (width <= 0) return;
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(left, bottom, width, HeaderHeight),
            Text = text,
            Style = _headerTextStyle,
            ZIndex = z,
        });
    }

    private void DrawPlaceholder(ICanvas c, RectF rect, string text, int z)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        c.DrawText(new DrawTextInputs
        {
            Position = rect,
            Text = text,
            Style = _placeholderStyle,
            ZIndex = z,
        });
    }

    private float ComputeGraphColumnWidth()
    {
        var laneCount = _snapshot?.LaneCount ?? 0;
        var lanes = Math.Min(Math.Max(laneCount, 1), MaxRenderedLanes);
        return GraphColumnPaddingLeft + lanes * LaneWidth + GraphColumnPaddingRight;
    }

    private RectF ComputeCommitsColumnRect(RectF body)
    {
        var dateX = body.Right - _dateColumnWidth - ColumnGap;
        var hashX = dateX - _hashColumnWidth - ColumnGap;
        var authorX = hashX - _authorColumnWidth - ColumnGap;
        var rightEdge = authorX - ColumnGap;
        var width = Math.Max(0f, rightEdge - body.Left);
        return new RectF(body.Left, body.Bottom, width, body.Height);
    }

    private void DrawCommits(ICanvas c, RectF body, int z)
    {
        var snap = _snapshot;
        if (snap == null || snap.Commits.Count == 0)
        {
            DrawPlaceholder(c, ComputeCommitsColumnRect(body), "No commits.", z);
            return;
        }

        ClampScroll(body, snap.Commits.Count);

        c.PushClip(body);

        var contentTop = body.Top;
        var bodyHeight = body.Height;
        var scrollY = _scrollY;
        var graphStartX = body.Left + GraphColumnPaddingLeft;
        var dateX = body.Right - _dateColumnWidth - ColumnGap;
        var hashX = dateX - _hashColumnWidth - ColumnGap;
        var authorX = hashX - _authorColumnWidth - ColumnGap;
        var authorPanelLeft = authorX - ColumnGap;
        var hashPanelLeft = hashX - ColumnGap;
        var datePanelLeft = dateX - ColumnGap;

        var firstVisible = Math.Max(0, (int)(scrollY / RowHeight) - 1);
        var lastVisible = Math.Min(snap.Commits.Count - 1, (int)((scrollY + bodyHeight) / RowHeight) + 1);

        for (var r = firstVisible; r <= lastVisible; r++)
        {
            var node = snap.Commits[r];
            var rowTop = contentTop + scrollY - r * RowHeight;
            var rowBottom = rowTop - RowHeight;
            if (rowTop <= body.Bottom || rowBottom >= contentTop) continue;

            DrawRowBackground(c, body, node, rowBottom, z);
            DrawGraphCell(c, node, graphStartX, rowBottom, z + 1);

            var textTop = rowBottom;
            var maxLaneAtRow = node.Lane;
            foreach (var l in node.PassThroughLanes) if (l > maxLaneAtRow) maxLaneAtRow = l;
            foreach (var l in node.IncomingLanes) if (l > maxLaneAtRow) maxLaneAtRow = l;
            foreach (var p in node.InWalkParentLanes) if (p.Lane > maxLaneAtRow) maxLaneAtRow = p.Lane;
            var summaryStartX = LaneCenterX(graphStartX, maxLaneAtRow) + DotRadius + GraphColumnPaddingRight;
            var refsEndX = DrawBadges(c, node, summaryStartX, textTop, z + 2);
            var summaryDraw = Math.Max(0, body.Right - refsEndX);
            DrawText(c, node.Summary, refsEndX, textTop, summaryDraw, node.Sha == _selectedSha, z + 2);

            var isSelected = node.Sha == _selectedSha;
            var rowOverlayColor = isSelected ? CommitsPalette.RowHighlight : CommitsPalette.Background;
            DrawColumnOverlay(c, authorPanelLeft, rowBottom, hashPanelLeft - authorPanelLeft, rowOverlayColor, z + 3);
            DrawText(c, node.Author, authorX, textTop, _authorColumnWidth, isSelected, z + 4);
            DrawColumnOverlay(c, hashPanelLeft, rowBottom, datePanelLeft - hashPanelLeft, rowOverlayColor, z + 5);
            DrawHashText(c, ShortSha(node.Sha), hashX, textTop, _hashColumnWidth, isSelected, z + 6);
            DrawColumnOverlay(c, datePanelLeft, rowBottom, body.Right - datePanelLeft, rowOverlayColor, z + 7);
            DrawText(c, FormatRelative(node.When), dateX, textTop, _dateColumnWidth, isSelected, z + 8);
        }

        c.PopClip();
    }

    private void DrawRowBackground(ICanvas c, RectF body, CommitNode node, float rowBottom, int z)
    {
        if (node.Sha != _selectedSha) return;
        var rect = new RectF(body.Left, rowBottom, body.Width, RowHeight);
        c.DrawRect(new DrawRectInputs
        {
            Position = rect,
            Style = new RectStyle { BackgroundColor = CommitsPalette.RowHighlight },
            ZIndex = z,
        });
    }

    private static float LaneCenterX(float graphStartX, int lane)
        => graphStartX + Math.Min(lane, MaxRenderedLanes - 1) * LaneWidth + LaneWidth * 0.5f;

    private void DrawGraphCell(ICanvas c, CommitNode node, float graphStartX, float rowBottom, int z)
    {
        var rowCenterY = rowBottom + RowHeight * 0.5f;
        var commitColor = CommitsPalette.LaneColor(node.Lane);
        var commitCx = LaneCenterX(graphStartX, node.Lane);

        // Pass-through verticals (lanes with no interaction at this row).
        foreach (var ptLane in node.PassThroughLanes)
        {
            DrawVertical(c, LaneCenterX(graphStartX, ptLane), rowBottom, RowHeight,
                CommitsPalette.LaneColor(ptLane), z);
        }

        // Top half of commit's own lane (only if an edge continues from above).
        if (node.HasIncomingAtCommitLane)
        {
            DrawVertical(c, commitCx, rowCenterY, RowHeight * 0.5f, commitColor, z);
        }

        // Incoming merge edges from other lanes above this commit.
        foreach (var inLane in node.IncomingLanes)
        {
            var inCx = LaneCenterX(graphStartX, inLane);
            var inColor = CommitsPalette.LaneColor(inLane);
            DrawVertical(c, inCx, rowCenterY, RowHeight * 0.5f, inColor, z);
            DrawHorizontal(c, Math.Min(inCx, commitCx), Math.Max(inCx, commitCx), rowCenterY, inColor, z);
        }

        // Outgoing edges to parents (continuation + branches).
        foreach (var pl in node.InWalkParentLanes)
        {
            var pCx = LaneCenterX(graphStartX, pl.Lane);
            var pColor = CommitsPalette.LaneColor(pl.Lane);
            if (pl.Lane == node.Lane)
            {
                DrawVertical(c, commitCx, rowBottom, RowHeight * 0.5f, commitColor, z);
            }
            else
            {
                DrawHorizontal(c, Math.Min(commitCx, pCx), Math.Max(commitCx, pCx), rowCenterY, pColor, z);
                DrawVertical(c, pCx, rowBottom, RowHeight * 0.5f, pColor, z);
            }
        }

        // The dot.
        var dotRect = new RectF(commitCx - DotRadius, rowCenterY - DotRadius, DotRadius * 2, DotRadius * 2);
        c.DrawRect(new DrawRectInputs
        {
            Position = dotRect,
            Style = new RectStyle
            {
                BackgroundColor = commitColor,
                BorderRadius = BorderRadiusStyle.All(DotRadius),
            },
            ZIndex = z + 1,
        });
    }

    private static void DrawVertical(ICanvas c, float cx, float bottomY, float height, uint color, int z)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(cx - EdgeThickness * 0.5f, bottomY, EdgeThickness, height),
            Style = new RectStyle { BackgroundColor = color },
            ZIndex = z,
        });
    }

    private static void DrawHorizontal(ICanvas c, float leftX, float rightX, float cy, uint color, int z)
    {
        var w = rightX - leftX;
        if (w <= 0) return;
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(leftX, cy - EdgeThickness * 0.5f, w, EdgeThickness),
            Style = new RectStyle { BackgroundColor = color },
            ZIndex = z,
        });
    }

    private float DrawBadges(ICanvas c, CommitNode node, float left, float rowBottom, int z)
    {
        if (node.Refs.Count == 0) return left;
        if (Context == null) return left;

        var x = left;
        var badgeY = rowBottom + (RowHeight - BadgeHeight) * 0.5f;
        foreach (var badge in node.Refs)
        {
            var textWidth = Context.Canvas.MeasureTextWidth(badge.Name, _badgeTextStyle);
            var badgeW = textWidth + BadgePaddingX * 2;
            var bg = badge.Kind switch
            {
                RefKind.LocalBranch => CommitsPalette.BadgeLocalBg,
                RefKind.RemoteBranch => CommitsPalette.BadgeRemoteBg,
                RefKind.Head => CommitsPalette.BadgeHeadBg,
                _ => CommitsPalette.BadgeLocalBg,
            };
            c.DrawRect(new DrawRectInputs
            {
                Position = new RectF(x, badgeY, badgeW, BadgeHeight),
                Style = new RectStyle
                {
                    BackgroundColor = bg,
                    BorderRadius = BorderRadiusStyle.All(3),
                },
                ZIndex = z,
            });
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(x + BadgePaddingX, badgeY, textWidth, BadgeHeight),
                Text = badge.Name,
                Style = _badgeTextStyle,
                ZIndex = z + 1,
            });
            x += badgeW + BadgeGap;
        }
        return x + BadgeGap;
    }

    private void DrawText(ICanvas c, string text, float left, float rowBottom, float width, bool active, int z)
    {
        if (width <= 0 || string.IsNullOrEmpty(text)) return;
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(left, rowBottom, width, RowHeight),
            Text = text,
            Style = active ? _rowTextActiveStyle : _rowTextStyle,
            ZIndex = z,
        });
    }

    private void DrawHashText(ICanvas c, string text, float left, float rowBottom, float width, bool active, int z)
    {
        if (width <= 0 || string.IsNullOrEmpty(text)) return;
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(left, rowBottom, width, RowHeight),
            Text = text,
            Style = active ? _hashTextActiveStyle : _hashTextStyle,
            ZIndex = z,
        });
    }

    private static string ShortSha(string sha)
        => string.IsNullOrEmpty(sha) ? string.Empty : (sha.Length >= 7 ? sha[..7] : sha);

    private void ClampScroll(RectF body, int commitCount)
    {
        var contentHeight = commitCount * RowHeight;
        var maxScroll = Math.Max(0f, contentHeight - body.Height);
        _scrollY = Math.Clamp(_scrollY, 0f, maxScroll);
        NotifyScrollChanged();
    }

    internal void OnWheel(float deltaY)
    {
        if (_state != CommitsLoadState.Loaded) return;
        _scrollY -= deltaY * ScrollWheelStep;
        var snap = _snapshot;
        if (snap != null)
        {
            var bodyHeight = Position.Height - HeaderHeight;
            var contentHeight = snap.Commits.Count * RowHeight;
            var maxScroll = Math.Max(0f, contentHeight - bodyHeight);
            if (_scrollY < 0f) _scrollY = 0f;
            if (_scrollY > maxScroll) _scrollY = maxScroll;
        }
        NotifyScrollChanged();
    }

    internal enum DividerKind
    {
        None,
        Author,
        Hash,
        Date,
    }

    internal DividerKind HitTestDivider(PointF point)
    {
        var pos = Position;
        if (point.X < pos.Left || point.X > pos.Right) return DividerKind.None;
        if (point.Y < pos.Bottom || point.Y > pos.Top) return DividerKind.None;

        var dateX = pos.Right - _dateColumnWidth - ColumnGap;
        var hashX = dateX - _hashColumnWidth - ColumnGap;
        var authorX = hashX - _authorColumnWidth - ColumnGap;
        var authorDividerX = authorX - ColumnGap;
        var hashDividerX = hashX - ColumnGap;
        var dateDividerX = dateX - ColumnGap;

        if (Math.Abs(point.X - dateDividerX) <= DividerHitWidth * 0.5f) return DividerKind.Date;
        if (Math.Abs(point.X - hashDividerX) <= DividerHitWidth * 0.5f) return DividerKind.Hash;
        if (Math.Abs(point.X - authorDividerX) <= DividerHitWidth * 0.5f) return DividerKind.Author;
        return DividerKind.None;
    }

    internal void ResizeAuthorColumn(float mouseDeltaX)
    {
        _authorColumnWidth = Math.Clamp(_authorColumnWidth - mouseDeltaX, MinColumnWidth, MaxColumnWidth);
    }

    internal void ResizeHashColumn(float mouseDeltaX)
    {
        TradeWidths(ref _hashColumnWidth, ref _authorColumnWidth, mouseDeltaX);
    }

    internal void ResizeDateColumn(float mouseDeltaX)
    {
        TradeWidths(ref _dateColumnWidth, ref _hashColumnWidth, mouseDeltaX);
    }

    private static void TradeWidths(ref float rightCol, ref float leftCol, float mouseDeltaX)
    {
        // Drag right (positive delta) shrinks the right column and grows the left column.
        // Keeps the previous-divider position fixed: only the two adjacent columns change.
        var shrink = mouseDeltaX;
        shrink = Math.Clamp(shrink, -(MaxColumnWidth - rightCol), rightCol - MinColumnWidth);
        shrink = Math.Clamp(shrink, -(leftCol - MinColumnWidth), MaxColumnWidth - leftCol);
        rightCol -= shrink;
        leftCol += shrink;
    }

    internal void SetHoveredDivider(DividerKind kind)
    {
        _hoveredDivider = kind;
    }

    internal void OnClickAt(PointF point)
    {
        if (_state != CommitsLoadState.Loaded) return;
        var snap = _snapshot;
        if (snap == null) return;

        var pos = Position;
        var bodyTop = pos.Top - HeaderHeight;
        if (point.Y > bodyTop || point.Y < pos.Bottom) return;
        if (point.X < pos.Left || point.X > pos.Right) return;

        var distFromTop = bodyTop - point.Y;
        var row = (int)((distFromTop + _scrollY) / RowHeight);
        if (row < 0 || row >= snap.Commits.Count) return;
        SetSelectedSha(snap.RepoId, snap.Commits[row].Sha);
    }

    private void SetSelectedSha(Guid repoId, string? sha)
    {
        if (_selectedSha == sha) return;
        _selectedSha = sha;
        _bus?.Broadcast(new CommitSelectedMessage(repoId, sha));
    }

    private void ClearSelection()
    {
        if (_selectedSha == null) return;
        _selectedSha = null;
        _bus?.Broadcast(new CommitSelectedMessage(_loadingRepoId, null));
    }

    private static string FormatRelative(DateTimeOffset when)
    {
        var now = DateTimeOffset.UtcNow;
        var delta = now - when;
        if (delta.TotalSeconds < 0) return when.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        if (delta.TotalMinutes < 1) return "just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays}d ago";
        if (delta.TotalDays < 30) return $"{(int)(delta.TotalDays / 7)}w ago";
        if (delta.TotalDays < 365) return $"{(int)(delta.TotalDays / 30)}mo ago";
        return $"{(int)(delta.TotalDays / 365)}y ago";
    }
}

internal sealed class CommitsViewController : KeyboardMouseController
{
    private readonly CommitsView _view;
    private CommitsView.DividerKind _activeDivider = CommitsView.DividerKind.None;
    private float _lastDragX;

    public CommitsViewController(CommitsView view)
    {
        _view = view;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.OnWheel(e.DeltaY);
        e.Consume();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button != MouseButton.Left) return;

        if (e.State == InputState.Pressed)
        {
            var divider = _view.HitTestDivider(e.Mouse.Point);
            if (divider != CommitsView.DividerKind.None)
            {
                _activeDivider = divider;
                _lastDragX = e.Mouse.Point.X;
                _view.Context?.Get<InputSystem>()?.RequestFocus(this);
                e.Consume();
                return;
            }
            _view.OnClickAt(e.Mouse.Point);
            e.Consume();
            return;
        }

        if (e.State == InputState.Released && _activeDivider != CommitsView.DividerKind.None)
        {
            _activeDivider = CommitsView.DividerKind.None;
            _view.Context?.Get<InputSystem>()?.Blur(this);
            e.Consume();
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (_activeDivider != CommitsView.DividerKind.None)
        {
            var dx = e.Mouse.Point.X - _lastDragX;
            _lastDragX = e.Mouse.Point.X;
            switch (_activeDivider)
            {
                case CommitsView.DividerKind.Author:
                    _view.ResizeAuthorColumn(dx);
                    break;
                case CommitsView.DividerKind.Hash:
                    _view.ResizeHashColumn(dx);
                    break;
                case CommitsView.DividerKind.Date:
                    _view.ResizeDateColumn(dx);
                    break;
            }
            e.Consume();
            return;
        }
        _view.SetHoveredDivider(_view.HitTestDivider(e.Mouse.Point));
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_activeDivider == CommitsView.DividerKind.None)
        {
            _view.SetHoveredDivider(CommitsView.DividerKind.None);
        }
    }
}
