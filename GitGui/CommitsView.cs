using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;

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
    private const float AuthorColumnWidth = 160f;
    private const float DateColumnWidth = 110f;
    private const float BadgePaddingX = 6f;
    private const float BadgeHeight = 16f;
    private const float BadgeGap = 4f;

    private IMessageBus? _bus;
    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IDisposable? _activeSubscription;

    private CommitsLoadState _state = CommitsLoadState.NoRepo;
    private CommitSnapshot? _snapshot;
    private CommitSnapshot? _pendingSnapshot;
    private int _loadGeneration;
    private Guid _loadingRepoId;

    private float _scrollY;
    private string? _selectedSha;

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
    private readonly TextStyle _rowTextDimStyle = new()
    {
        TextColor = CommitsPalette.RowTextDim,
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

    public CommitsView()
    {
        Behaviors.Add(new CommitsViewController(this));
    }

    protected override void OnAttachedToContext(Context context)
    {
        _bus = context.Get<IMessageBus>();
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        if (_registry != null)
        {
            _activeSubscription = _registry.Active.Subscribe(_ => StartLoadForActiveRepo());
        }
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _activeSubscription?.Dispose();
        _activeSubscription = null;
        _bus = null;
        _registry = null;
        _gitService = null;
    }

    private void StartLoadForActiveRepo()
    {
        if (_registry == null || _gitService == null) return;
        var active = _registry.Active.Value;

        _loadGeneration++;
        var gen = _loadGeneration;

        if (active == null)
        {
            _state = CommitsLoadState.NoRepo;
            _snapshot = null;
            _scrollY = 0f;
            _selectedSha = null;
            return;
        }

        _state = CommitsLoadState.Loading;
        _snapshot = null;
        _scrollY = 0f;
        _selectedSha = null;
        _loadingRepoId = active.Id;

        var repo = active;
        var service = _gitService;
        Task.Run(() =>
        {
            try
            {
                var snap = service.Load(repo, 5000);
                if (gen != Volatile.Read(ref _loadGeneration)) return;
                Volatile.Write(ref _pendingSnapshot, snap);
            }
            catch (Exception ex)
            {
                if (gen != Volatile.Read(ref _loadGeneration)) return;
                Volatile.Write(ref _pendingSnapshot,
                    new CommitSnapshot(repo.Id, repo.Path, Array.Empty<CommitNode>(), 0, false, ex.Message));
            }
        });
    }

    private void PollPending()
    {
        var pending = Interlocked.Exchange(ref _pendingSnapshot, null);
        if (pending == null) return;
        if (pending.RepoId != _loadingRepoId) return;
        _snapshot = pending;
        _state = pending.ErrorMessage != null ? CommitsLoadState.Error : CommitsLoadState.Loaded;
        _scrollY = 0f;
        _bus?.Broadcast(new CommitsLoadedMessage(pending.RepoId));
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        PollPending();

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
                DrawPlaceholder(c, bodyRect, "Select a repository to view its history.", z + 2);
                break;
            case CommitsLoadState.Loading:
                DrawPlaceholder(c, bodyRect, "Loading…", z + 2);
                break;
            case CommitsLoadState.Error:
                DrawPlaceholder(c, bodyRect, _snapshot?.ErrorMessage ?? "Error.", z + 2);
                break;
            case CommitsLoadState.Loaded:
                DrawCommits(c, bodyRect, z + 2);
                break;
        }

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
        var dateX = pos.Right - DateColumnWidth - ColumnGap;
        var authorX = dateX - AuthorColumnWidth - ColumnGap;

        DrawHeaderText(c, "Graph", pos.Left + GraphColumnPaddingLeft, pos.Top - HeaderHeight, graphWidth, z + 1);
        DrawHeaderText(c, "Author", authorX, pos.Top - HeaderHeight, AuthorColumnWidth, z + 1);
        DrawHeaderText(c, "Date", dateX, pos.Top - HeaderHeight, DateColumnWidth, z + 1);
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

    private void DrawCommits(ICanvas c, RectF body, int z)
    {
        var snap = _snapshot;
        if (snap == null || snap.Commits.Count == 0)
        {
            DrawPlaceholder(c, body, "No commits.", z);
            return;
        }

        ClampScroll(body, snap.Commits.Count);

        c.PushClip(body);

        var contentTop = body.Top;
        var bodyHeight = body.Height;
        var scrollY = _scrollY;
        var graphStartX = body.Left + GraphColumnPaddingLeft;
        var dateX = body.Right - DateColumnWidth - ColumnGap;
        var authorX = dateX - AuthorColumnWidth - ColumnGap;

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
            var summaryLimitX = authorX - ColumnGap;
            var refsEndX = DrawBadges(c, node, summaryStartX, textTop, z + 2);
            var summaryDraw = Math.Max(0, summaryLimitX - refsEndX);
            DrawText(c, node.Summary, refsEndX, textTop, summaryDraw, node.Sha == _selectedSha, z + 2);
            DrawText(c, node.Author, authorX, textTop, AuthorColumnWidth, node.Sha == _selectedSha, z + 2);
            DrawText(c, FormatRelative(node.When), dateX, textTop, DateColumnWidth, node.Sha == _selectedSha, z + 2);
        }

        if (snap.Truncated)
        {
            var msg = new RectF(body.Left, body.Bottom, body.Width, 18);
            c.DrawText(new DrawTextInputs
            {
                Position = msg,
                Text = $"History truncated at {snap.Commits.Count} commits.",
                Style = _rowTextDimStyle,
                ZIndex = z + 5,
            });
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

    private void ClampScroll(RectF body, int commitCount)
    {
        var contentHeight = commitCount * RowHeight;
        var maxScroll = Math.Max(0f, contentHeight - body.Height);
        if (_scrollY < 0f) _scrollY = 0f;
        if (_scrollY > maxScroll) _scrollY = maxScroll;
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
        _selectedSha = snap.Commits[row].Sha;
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
        if (e.Button != MouseButton.Left || e.State != InputState.Pressed) return;
        _view.OnClickAt(e.Mouse.Point);
        e.Consume();
    }
}
