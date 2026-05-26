using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

public sealed class CommitDetailsView : MultiChildView, IBind<CommitDetailsViewModel>
{
    private const int Padding = 14;
    private const float AvatarSize = 36f;

    private readonly ColumnView _content;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;
    private readonly DiffView _diffView;
    private readonly VerticalSplitContainer _splitContainer;
    private readonly State<string?> _selectedPath = new(null);
    private CommitDetailsViewModel? _vm;

    public CommitDetailsView()
    {
        // _content is unpadded so children can opt into edge-to-edge layout. The author/
        // message/metadata block wraps itself in a PaddingView; FileChangesSection stays
        // full-width so its header bar spans edge-to-edge.
        _content = new ColumnView { Gap = 8 };

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(_content);
        _scrollPane.UseController(_ => new ScrollPaneWheelController(_scrollPane));

        _vScrollBar = ScrollBarStyles.CreateVertical();
        _hScrollBar = ScrollBarStyles.CreateHorizontal();

        var topHalf = new BorderLayoutView
        {
            Center = _scrollPane,
            East = _vScrollBar,
            South = _hScrollBar,
        };

        _diffView = new DiffView();

        var splitterHovered = new State<bool>(false);
        var splitterHover = new State<uint>(ThemePresets.Dark.Commits.DividerHoverBg);
        var splitterIdle = new State<uint>(ThemePresets.Dark.Commits.Border);
        var splitter = new RectView();
        splitter.BindToTheme(t =>
        {
            splitterHover.Value = t.Commits.DividerHoverBg;
            splitterIdle.Value = t.Commits.Border;
        });
        splitter.BindBackgroundColor(() => splitterHovered.Value ? splitterHover.Value : splitterIdle.Value);

        _splitContainer = new VerticalSplitContainer(topHalf, _diffView, splitter, bottomFraction: 1f / 2f);

        splitter.UseController(ctx => new SplitterController(
            ctx,
            DragAxis.Y,
            _splitContainer.AdjustBottomFractionByPixels,
            h => splitterHovered.Value = h));

        var frame = new RectView
        {
            BorderSize = new BorderSizeStyle { Left = 1 },
            Children = { _splitContainer },
        };
        frame.BindBackgroundColorFromTheme(t => t.CommitDetails.Background);
        frame.BindBorderColorFromTheme(t => new BorderColorStyle { Left = t.CommitDetails.Border });
        AddChildToSelf(frame);

        this.UseController(_ => new ScrollSyncController(_scrollPane, _vScrollBar, _hScrollBar));

        this.UseViewModel(this);
    }

    public void Bind(CommitDetailsViewModel vm)
    {
        _vm = vm;
        vm.RenderState.Subscribe(SetRenderState);
        vm.SelectedTarget.Subscribe(target =>
        {
            _diffView.SetTarget(target?.Path, target?.Side ?? DiffSide.Commit, target?.CommitSha);
            ApplyDiffVisibility(target != null, _diffView.IsCollapsed.Value);
        });
        vm.SelectedPath.Subscribe(p => _selectedPath.Value = p);
        _diffView.IsCollapsed.Subscribe(collapsed =>
            ApplyDiffVisibility(vm.SelectedTarget.Value != null, collapsed));
    }

    private void ApplyDiffVisibility(bool hasTarget, bool collapsed)
    {
        _splitContainer.BottomVisible = hasTarget;
        _splitContainer.SetBottomCollapsed(hasTarget && collapsed, DiffView.HeaderHeight);
    }

    private void SetRenderState(CommitDetailsRenderState state)
    {
        switch (state)
        {
            case CommitDetailsRenderState.Placeholder p:
                ShowPlaceholder(p.Text);
                break;
            case CommitDetailsRenderState.Loaded l:
                ShowDetails(l.Details);
                break;
        }
    }

    private void ShowPlaceholder(string text)
    {
        _content.Children.Clear();
        var placeholder = new TextView
        {
            Text = text,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        placeholder.BindTextColorFromTheme(t => t.CommitDetails.Placeholder);
        _content.Children.Add(placeholder);
        _scrollPane.ScrollToOrigin();
    }

    private void ShowDetails(CommitDetails d)
    {
        _content.Children.Clear();

        var topColumn = new ColumnView { Gap = 8 };
        topColumn.Children.Add(BuildAuthorHeader(d));

        if (!string.IsNullOrEmpty(d.MessageShort))
        {
            var subject = new TextView { Text = d.MessageShort };
            subject.BindTextColorFromTheme(t => t.CommitDetails.Primary);
            topColumn.Children.Add(subject);
        }

        var body = ExtractBody(d.Message, d.MessageShort);
        if (!string.IsNullOrEmpty(body))
        {
            var bodyView = new TextView { Text = body };
            bodyView.BindTextColorFromTheme(t => t.CommitDetails.Secondary);
            topColumn.Children.Add(bodyView);
        }

        var shaLine = new TextView { Text = $"Commit:  {d.Sha}" };
        shaLine.BindTextColorFromTheme(t => t.CommitDetails.Muted);
        topColumn.Children.Add(shaLine);

        var parentLine = new TextView
        {
            Text = d.ParentShas.Count == 0
                ? "Parents: (none)"
                : "Parents: " + string.Join(", ", d.ParentShas.Select(ShortSha)),
        };
        parentLine.BindTextColorFromTheme(t => t.CommitDetails.Muted);
        topColumn.Children.Add(parentLine);

        _content.Children.Add(new PaddingView
        {
            Padding = new PaddingStyle { Left = Padding, Right = Padding, Top = Padding, Bottom = 0 },
            Children = { topColumn },
        });

        var changesSection = new FileChangesSection(
            "Changes",
            selectedPath: _selectedPath,
            onRowClicked: f => _vm?.SelectFile(f.Path));
        changesSection.SetFiles(d.Files);
        _content.Children.Add(changesSection);

        _scrollPane.ScrollToOrigin();
    }

    private static View BuildAuthorHeader(CommitDetails d)
    {
        var avatarSeed = !string.IsNullOrEmpty(d.AuthorEmail) ? d.AuthorEmail : d.AuthorName;
        var initialsView = new TextView
        {
            Text = Initials(d.AuthorName, d.AuthorEmail),
            TextColor = 0xFFFFFFFF,
            FontSize = 16f,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var avatar = new RectView
        {
            PreferredWidth = AvatarSize,
            PreferredHeight = AvatarSize,
            BorderRadius = BorderRadiusStyle.All(AvatarSize * 0.5f),
            Children = { initialsView },
        };
        avatar.BindBackgroundColorFromTheme(t => t.CommitDetails.AvatarColor(avatarSeed));

        var nameView = new TextView { Text = FormatAuthor(d.AuthorName, d.AuthorEmail) };
        nameView.BindTextColorFromTheme(t => t.CommitDetails.Primary);

        var dateView = new TextView { Text = FormatFullDate(d.AuthorWhen) };
        dateView.BindTextColorFromTheme(t => t.CommitDetails.Muted);

        var info = new ColumnView
        {
            Gap = 2,
            Children = { nameView, dateView },
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
        if (e.DeltaY != 0f) _pane.ScrollVertical(-e.DeltaY * Step);
        if (e.DeltaX != 0f) _pane.ScrollHorizontal(-e.DeltaX * Step);
        e.Consume();
    }
}
