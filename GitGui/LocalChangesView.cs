using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

internal enum LocalChangesState
{
    NoRepo,
    Loading,
    Loaded,
    Error,
}

public sealed class LocalChangesView : MultiChildView
{
    private const int Padding = 14;

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IDisposable? _activeSubscription;

    private LocalChangesState _state = LocalChangesState.NoRepo;
    private LocalChangesSnapshot? _pendingSnapshot;
    private string? _pendingErrorMessage;
    private int _loadGeneration;
    private Guid _loadingRepoId;

    private readonly ColumnView _content;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly TextView _placeholder;
    private readonly FileChangesSection _stagedSection;
    private readonly FileChangesSection _unstagedSection;

    public LocalChangesView()
    {
        _placeholder = new TextView
        {
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        _stagedSection = new FileChangesSection("Staged", emptyText: "No staged changes.");
        _unstagedSection = new FileChangesSection("Unstaged", emptyText: "No unstaged changes.");

        _content = new ColumnView { Gap = 12 };
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
        _vScrollBar.Thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        _vScrollBar.Thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        _vScrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        _vScrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);
        _vScrollBar.Behaviors.Add(new VerticalScrollBarViewController(_vScrollBar));

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children =
            {
                new BorderLayoutView
                {
                    Center = _scrollPane,
                    East = _vScrollBar,
                },
            },
        });

        Behaviors.Add(new LocalChangesScrollSyncController(_scrollPane, _vScrollBar));

        ShowPlaceholder("Open a repository to see local changes.");
    }

    protected override void OnAttachedToContext(Context context)
    {
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
            _state = LocalChangesState.NoRepo;
            ShowPlaceholder("Open a repository to see local changes.");
            return;
        }

        _state = LocalChangesState.Loading;
        _loadingRepoId = active.Id;
        ShowPlaceholder("Loading…");

        var repo = active;
        var service = _gitService;
        Task.Run(() =>
        {
            try
            {
                var snap = service.GetLocalChanges(repo);
                if (gen != Volatile.Read(ref _loadGeneration)) return;
                Volatile.Write(ref _pendingSnapshot, snap);
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
            _state = LocalChangesState.Error;
            ShowPlaceholder(err);
        }

        var pending = Interlocked.Exchange(ref _pendingSnapshot, null);
        if (pending == null) return;
        if (pending.RepoId != _loadingRepoId) return;

        if (pending.ErrorMessage != null)
        {
            _state = LocalChangesState.Error;
            ShowPlaceholder(pending.ErrorMessage);
            return;
        }

        _state = LocalChangesState.Loaded;
        ShowSnapshot(pending);
    }

    private void ShowPlaceholder(string text)
    {
        _placeholder.Text = text;
        _content.Children.Clear();
        _content.Children.Add(_placeholder);
        _scrollPane.ScrollToOrigin();
    }

    private void ShowSnapshot(LocalChangesSnapshot snap)
    {
        _stagedSection.SetFiles(snap.Staged);
        _unstagedSection.SetFiles(snap.Unstaged);
        _content.Children.Clear();
        _content.Children.Add(_stagedSection);
        _content.Children.Add(_unstagedSection);
        _scrollPane.ScrollToOrigin();
    }
}

internal sealed class LocalChangesScrollSyncController : KeyboardMouseController
{
    private const float ScrollBarThickness = 12f;

    private readonly ScrollPane _pane;
    private readonly VerticalScrollBarView _vScrollBar;

    public LocalChangesScrollSyncController(ScrollPane pane, VerticalScrollBarView vScrollBar)
    {
        _pane = pane;
        _vScrollBar = vScrollBar;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged += OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged += OnVScrollBarScroll;
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged -= OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged -= OnVScrollBarScroll;
    }

    private void OnPaneVerticalScroll(float normalized)
    {
        _vScrollBar.PreferredWidth = _pane.VerticalScale < 1f ? ScrollBarThickness : 0f;
        _vScrollBar.Scale = _pane.VerticalScale;
        _vScrollBar.SetNormalizedScrollPosition(normalized);
    }

    private void OnVScrollBarScroll(float normalized)
    {
        _pane.SetVerticalNormalizedScrollPosition(normalized);
    }
}
