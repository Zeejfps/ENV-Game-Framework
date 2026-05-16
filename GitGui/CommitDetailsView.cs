using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

internal static class CommitDetailsPalette
{
    public const uint Background = 0xFF1A1B1E;
    public const uint Border = 0xFF313338;
    public const uint SectionBg = 0xFF222326;
    public const uint Heading = 0xFF96989D;
    public const uint Primary = 0xFFE6E6E6;
    public const uint Secondary = 0xFFB5B9C0;
    public const uint Muted = 0xFF7A7C81;
    public const uint Placeholder = 0xFF96989D;

    public const uint StatusAdded = 0xFF57F287;
    public const uint StatusModified = 0xFFE9C77A;
    public const uint StatusDeleted = 0xFFED4245;
    public const uint StatusRenamed = 0xFF5DADE2;
    public const uint StatusOther = 0xFF9B59B6;

    private static readonly uint[] AvatarPalette =
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

    public static uint AvatarColor(string seed)
    {
        if (string.IsNullOrEmpty(seed)) return AvatarPalette[0];
        var h = 0;
        foreach (var ch in seed) h = unchecked(h * 31 + char.ToLowerInvariant(ch));
        var idx = ((h % AvatarPalette.Length) + AvatarPalette.Length) % AvatarPalette.Length;
        return AvatarPalette[idx];
    }
}

internal enum CommitDetailsState
{
    Empty,
    Loading,
    Loaded,
    Error,
}

public sealed class CommitDetailsView : MultiChildView
{
    private const int Padding = 14;
    private const float AvatarSize = 36f;
    private const float StatusBadgeSize = 16f;

    private IMessageBus? _bus;
    private IGitService? _gitService;
    private IRepoRegistry? _registry;
    private Action<CommitSelectedMessage>? _selectedHandler;

    private CommitDetailsState _state = CommitDetailsState.Empty;
    private CommitDetails? _details;
    private CommitDetails? _pendingDetails;
    private string? _pendingErrorMessage;
    private int _loadGeneration;
    private string? _requestedSha;
    private Guid _requestedRepoId;

    private readonly ColumnView _content;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    public CommitDetailsView()
    {
        _content = new ColumnView { Gap = 8 };
        var paddedContent = new RectView
        {
            Padding = new PaddingStyle { Left = Padding, Right = Padding, Top = Padding, Bottom = Padding },
            Children = { _content },
        };

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(paddedContent);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _vScrollBar = new VerticalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        StyleScrollBarThumb(_vScrollBar.Thumb);
        _vScrollBar.Behaviors.Add(new VerticalScrollBarViewController(_vScrollBar));

        _hScrollBar = new HorizontalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Top = 1 },
        };
        StyleHorizontalScrollBarThumb(_hScrollBar.Thumb);
        _hScrollBar.Behaviors.Add(new HorizontalScrollBarViewController(_hScrollBar));

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitDetailsPalette.Background,
            BorderColor = new BorderColorStyle { Left = CommitDetailsPalette.Border },
            BorderSize = new BorderSizeStyle { Left = 1 },
            Children =
            {
                new BorderLayoutView
                {
                    Center = _scrollPane,
                    East = _vScrollBar,
                    South = _hScrollBar,
                },
            },
        });

        Behaviors.Add(new CommitDetailsScrollSyncController(_scrollPane, _vScrollBar, _hScrollBar));

        ShowPlaceholder("Select a commit to view details.");
    }

    private static void StyleScrollBarThumb(VerticalScrollBarThumbView thumb)
    {
        thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        thumb.BorderSize = BorderSizeStyle.All(1);
    }

    private static void StyleHorizontalScrollBarThumb(HorizontalScrollBarThumbView thumb)
    {
        thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        thumb.BorderSize = BorderSizeStyle.All(1);
    }

    protected override void OnAttachedToContext(Context context)
    {
        _bus = context.Get<IMessageBus>();
        _gitService = context.Get<IGitService>();
        _registry = context.Get<IRepoRegistry>();
        if (_bus != null)
        {
            _selectedHandler = OnCommitSelected;
            _bus.Subscribe(_selectedHandler);
        }
    }

    protected override void OnDetachedFromContext(Context context)
    {
        if (_bus != null && _selectedHandler != null)
        {
            _bus.Unsubscribe(_selectedHandler);
        }
        _selectedHandler = null;
        _bus = null;
        _gitService = null;
        _registry = null;
    }

    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Sha))
        {
            _loadGeneration++;
            _requestedSha = null;
            _state = CommitDetailsState.Empty;
            _details = null;
            ShowPlaceholder("Select a commit to view details.");
            return;
        }
        StartLoad(msg.RepoId, msg.Sha);
    }

    private void StartLoad(Guid repoId, string sha)
    {
        if (_gitService == null || _registry == null) return;
        var repo = _registry.Active.Value;
        if (repo == null || repo.Id != repoId) return;

        _loadGeneration++;
        var gen = _loadGeneration;
        _requestedSha = sha;
        _requestedRepoId = repoId;
        _state = CommitDetailsState.Loading;
        _details = null;
        ShowPlaceholder("Loading…");

        var service = _gitService;
        Task.Run(() =>
        {
            try
            {
                var details = service.LoadDetails(repo, sha);
                if (gen != Volatile.Read(ref _loadGeneration)) return;
                Volatile.Write(ref _pendingDetails, details);
            }
            catch (Exception ex)
            {
                if (gen != Volatile.Read(ref _loadGeneration)) return;
                Volatile.Write(ref _pendingErrorMessage, ex.Message);
            }
        });
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        PollPending();
    }

    private void PollPending()
    {
        var err = Interlocked.Exchange(ref _pendingErrorMessage, null);
        if (err != null)
        {
            _details = null;
            _state = CommitDetailsState.Error;
            ShowPlaceholder(err);
        }

        var pending = Interlocked.Exchange(ref _pendingDetails, null);
        if (pending == null) return;
        if (pending.Sha != _requestedSha || pending.RepoId != _requestedRepoId) return;
        _details = pending;
        _state = pending.ErrorMessage != null ? CommitDetailsState.Error : CommitDetailsState.Loaded;
        if (_state == CommitDetailsState.Error)
            ShowPlaceholder(pending.ErrorMessage ?? "Error.");
        else
            ShowDetails(pending);
    }

    private void ShowPlaceholder(string text)
    {
        _content.Children.Clear();
        _content.Children.Add(new TextView
        {
            Text = text,
            TextColor = CommitDetailsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
        });
        _scrollPane.ScrollToOrigin();
    }

    private void ShowDetails(CommitDetails d)
    {
        _content.Children.Clear();

        // Author header: avatar + Name <email> / date column
        _content.Children.Add(BuildAuthorHeader(d));

        // Commit message: subject + body, both wrapped
        if (!string.IsNullOrEmpty(d.MessageShort))
        {
            _content.Children.Add(new TextView
            {
                Text = d.MessageShort,
                TextColor = CommitDetailsPalette.Primary,
                });
        }

        var body = ExtractBody(d.Message, d.MessageShort);
        if (!string.IsNullOrEmpty(body))
        {
            _content.Children.Add(new TextView
            {
                Text = body,
                TextColor = CommitDetailsPalette.Secondary,
            });
        }

        // Metadata
        _content.Children.Add(new TextView
        {
            Text = $"Commit:  {d.Sha}",
            TextColor = CommitDetailsPalette.Muted,
        });
        _content.Children.Add(new TextView
        {
            Text = d.ParentShas.Count == 0
                ? "Parents: (none)"
                : "Parents: " + string.Join(", ", d.ParentShas.Select(ShortSha)),
            TextColor = CommitDetailsPalette.Muted,
        });

        // Files header bar
        _content.Children.Add(BuildFilesHeader(d.Files.Count));

        // File rows
        foreach (var file in d.Files)
        {
            _content.Children.Add(BuildFileRow(file));
        }

        _scrollPane.ScrollToOrigin();
    }

    private static View BuildAuthorHeader(CommitDetails d)
    {
        var avatarSeed = !string.IsNullOrEmpty(d.AuthorEmail) ? d.AuthorEmail : d.AuthorName;
        var avatar = new RectView
        {
            PreferredWidth = AvatarSize,
            PreferredHeight = AvatarSize,
            BackgroundColor = CommitDetailsPalette.AvatarColor(avatarSeed),
            BorderRadius = BorderRadiusStyle.All(AvatarSize * 0.5f),
            Children =
            {
                new TextView
                {
                    Text = Initials(d.AuthorName, d.AuthorEmail),
                    TextColor = 0xFFFFFFFF,
                    FontSize = 16f,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            },
        };

        var info = new ColumnView
        {
            Gap = 2,
            Children =
            {
                new TextView
                {
                    Text = FormatAuthor(d.AuthorName, d.AuthorEmail),
                    TextColor = CommitDetailsPalette.Primary,
                        },
                new TextView
                {
                    Text = FormatFullDate(d.AuthorWhen),
                    TextColor = CommitDetailsPalette.Muted,
                        },
            },
        };

        return new FlexRowView
        {
            Gap = 12f,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                avatar,
                new FlexItem { Grow = 1, Child = info },
            },
        };
    }

    private static View BuildFilesHeader(int count)
    {
        return new RectView
        {
            BackgroundColor = CommitDetailsPalette.SectionBg,
            BorderColor = new BorderColorStyle
            {
                Top = CommitDetailsPalette.Border,
                Bottom = CommitDetailsPalette.Border,
            },
            BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
            Padding = new PaddingStyle { Left = 4, Right = 4, Top = 4, Bottom = 4 },
            Children =
            {
                new TextView
                {
                    Text = $"Changes ({count})",
                    TextColor = CommitDetailsPalette.Heading,
                },
            },
        };
    }

    private static View BuildFileRow(FileChange file)
    {
        var color = file.Status switch
        {
            FileChangeStatus.Added => CommitDetailsPalette.StatusAdded,
            FileChangeStatus.Modified => CommitDetailsPalette.StatusModified,
            FileChangeStatus.Deleted => CommitDetailsPalette.StatusDeleted,
            FileChangeStatus.Renamed => CommitDetailsPalette.StatusRenamed,
            _ => CommitDetailsPalette.StatusOther,
        };

        var badge = new RectView
        {
            PreferredWidth = StatusBadgeSize,
            PreferredHeight = StatusBadgeSize,
            BackgroundColor = color,
            BorderRadius = BorderRadiusStyle.All(3),
            Children =
            {
                new TextView
                {
                    Text = StatusGlyph(file.Status),
                    TextColor = 0xFF1A1B1E,
                    FontSize = 11f,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            },
        };

        var path = new TextView
        {
            Text = FormatPath(file),
            TextColor = CommitDetailsPalette.Secondary,
        };

        return new FlexRowView
        {
            Gap = 8f,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                badge,
                new FlexItem { Grow = 1, Child = path },
            },
        };
    }

    private static string Initials(string name, string email)
    {
        var source = !string.IsNullOrWhiteSpace(name) ? name : email;
        if (string.IsNullOrWhiteSpace(source)) return "?";
        var parts = source.Split(new[] { ' ', '.', '_', '-', '@' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return char.ToUpperInvariant(parts[0][0]).ToString();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
    }

    private static string FormatAuthor(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return name;
        if (string.IsNullOrWhiteSpace(name)) return email;
        return $"{name} <{email}>";
    }

    private static string FormatFullDate(DateTimeOffset when)
    {
        if (when == DateTimeOffset.MinValue) return string.Empty;
        return when.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz");
    }

    private static string ShortSha(string sha)
        => string.IsNullOrEmpty(sha) ? string.Empty : (sha.Length >= 7 ? sha[..7] : sha);

    private static string StatusGlyph(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => "A",
        FileChangeStatus.Modified => "M",
        FileChangeStatus.Deleted => "D",
        FileChangeStatus.Renamed => "R",
        FileChangeStatus.Copied => "C",
        FileChangeStatus.TypeChanged => "T",
        _ => "·",
    };

    private static string FormatPath(FileChange file)
    {
        if (file.Status == FileChangeStatus.Renamed && !string.IsNullOrEmpty(file.OldPath))
            return $"{file.OldPath} → {file.Path}";
        return file.Path;
    }

    /// <summary>
    /// libgit2's Message includes the subject line. Extract the body (everything after the
    /// blank line after the subject) so we don't show the subject twice.
    /// </summary>
    private static string ExtractBody(string fullMessage, string subject)
    {
        if (string.IsNullOrEmpty(fullMessage)) return string.Empty;
        var normalized = fullMessage.Replace("\r\n", "\n");
        if (!string.IsNullOrEmpty(subject) && normalized.StartsWith(subject))
        {
            var rest = normalized.AsSpan(subject.Length).TrimStart('\n');
            return rest.ToString().TrimEnd();
        }
        return normalized.TrimEnd();
    }
}

internal sealed class ScrollPaneWheelController : KeyboardMouseController
{
    private const float Step = 60f;
    private readonly ScrollPane _pane;

    public ScrollPaneWheelController(ScrollPane pane)
    {
        _pane = pane;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        // Wheel scrolls vertically. Shift+wheel scrolls horizontally would be a future addition.
        _pane.ScrollVertical(-e.DeltaY * Step);
        e.Consume();
    }
}

internal sealed class CommitDetailsScrollSyncController : KeyboardMouseController
{
    private const float ScrollBarThickness = 12f;

    private readonly ScrollPane _pane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    public CommitDetailsScrollSyncController(ScrollPane pane, VerticalScrollBarView vScrollBar, HorizontalScrollBarView hScrollBar)
    {
        _pane = pane;
        _vScrollBar = vScrollBar;
        _hScrollBar = hScrollBar;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged += OnPaneVerticalScroll;
        _pane.HorizontalScrollPositionChanged += OnPaneHorizontalScroll;
        _vScrollBar.ScrollPositionChanged += OnVScrollBarScroll;
        _hScrollBar.ScrollPositionChanged += OnHScrollBarScroll;
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged -= OnPaneVerticalScroll;
        _pane.HorizontalScrollPositionChanged -= OnPaneHorizontalScroll;
        _vScrollBar.ScrollPositionChanged -= OnVScrollBarScroll;
        _hScrollBar.ScrollPositionChanged -= OnHScrollBarScroll;
    }

    private void OnPaneVerticalScroll(float normalized)
    {
        // Collapse the bar to zero width when content fits, so BorderLayout gives the saved
        // space to the center pane. Stable: removing a bar can only enlarge the viewport,
        // which can only keep scale at <=1 — never re-introduces overflow on this axis.
        _vScrollBar.PreferredWidth = _pane.VerticalScale < 1f ? ScrollBarThickness : 0f;
        _vScrollBar.Scale = _pane.VerticalScale;
        _vScrollBar.SetNormalizedScrollPosition(normalized);
    }

    private void OnPaneHorizontalScroll(float normalized)
    {
        _hScrollBar.PreferredHeight = _pane.HorizontalScale < 1f ? ScrollBarThickness : 0f;
        _hScrollBar.Scale = _pane.HorizontalScale;
        _hScrollBar.SetNormalizedScrollPosition(normalized);
    }

    private void OnVScrollBarScroll(float normalized)
    {
        _pane.SetVerticalNormalizedScrollPosition(normalized);
    }

    private void OnHScrollBarScroll(float normalized)
    {
        _pane.SetHorizontalNormalizedScrollPosition(normalized);
    }
}
