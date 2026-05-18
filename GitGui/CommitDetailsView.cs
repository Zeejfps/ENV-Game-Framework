using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal static class CommitDetailsPalette
{
    public const uint Background = Theme.BgDeep;
    public const uint Border = Theme.Border;
    public const uint Primary = Theme.TextPrimary;
    public const uint Secondary = Theme.TextRow;
    public const uint Muted = Theme.TextDim;
    public const uint Placeholder = Theme.TextHeader;

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

public sealed class CommitDetailsView : MultiChildView, ICommitDetailsView
{
    private const int Padding = 14;
    private const float AvatarSize = 36f;

    private readonly ColumnView _content;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    public CommitDetailsView()
    {
        _content = new ColumnView { Gap = 8 };
        var paddedContent = new PaddingView
        {
            Padding = new PaddingStyle { Left = Padding, Right = Padding, Top = Padding, Bottom = Padding },
            Children = { _content },
        };

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(paddedContent);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _vScrollBar = ScrollBarStyles.CreateVertical();
        _hScrollBar = ScrollBarStyles.CreateHorizontal();

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

        this.UsePresenter(ctx => new CommitDetailsPresenter(
            this,
            ctx.Require<IGitService>(),
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public void ShowPlaceholder(string text)
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

    public void ShowDetails(CommitDetails d)
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

        // Changed files
        var section = new FileChangesSection("Changes");
        section.SetFiles(d.Files);
        _content.Children.Add(section);

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
        _vScrollBar.ScrollPositionChanged += _pane.SetVerticalNormalizedScrollPosition;
        _hScrollBar.ScrollPositionChanged += _pane.SetHorizontalNormalizedScrollPosition;
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged -= OnPaneVerticalScroll;
        _pane.HorizontalScrollPositionChanged -= OnPaneHorizontalScroll;
        _vScrollBar.ScrollPositionChanged -= _pane.SetVerticalNormalizedScrollPosition;
        _hScrollBar.ScrollPositionChanged -= _pane.SetHorizontalNormalizedScrollPosition;
    }

    private void OnPaneVerticalScroll(float normalized)
        => ScrollBarSync.ApplyVertical(_vScrollBar, _pane.VerticalScale, normalized);

    private void OnPaneHorizontalScroll(float normalized)
        => ScrollBarSync.ApplyHorizontal(_hScrollBar, _pane.HorizontalScale, normalized);
}
