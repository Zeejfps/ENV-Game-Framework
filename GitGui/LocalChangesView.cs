using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal abstract record LocalChangesViewModel
{
    public sealed record Placeholder(string Text) : LocalChangesViewModel;
    public sealed record Loaded(LocalChangesSnapshot Snapshot) : LocalChangesViewModel;
}

public sealed class LocalChangesView : MultiChildView
{
    private const int Padding = 14;

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IDisposable? _activeSubscription;
    private IDisposable? _vmSubscription;

    private readonly State<LocalChangesViewModel> _viewModel = new(
        new LocalChangesViewModel.Placeholder("Open a repository to see local changes."));

    private int _loadGeneration;

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
    }

    protected override void OnAttachedToContext(Context context)
    {
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _vmSubscription = _viewModel.Subscribe(Render);
        if (_registry != null)
            _activeSubscription = _registry.Active.Subscribe(_ => StartLoadForActiveRepo());
    }

    protected override void OnDetachedFromContext(Context context)
    {
        // Bump the generation so any in-flight worker's dispatcher.Post becomes a no-op.
        _loadGeneration++;
        _activeSubscription?.Dispose();
        _activeSubscription = null;
        _vmSubscription?.Dispose();
        _vmSubscription = null;
        _registry = null;
        _gitService = null;
        _dispatcher = null;
    }

    private void StartLoadForActiveRepo()
    {
        if (_registry == null || _gitService == null) return;
        var active = _registry.Active.Value;

        _loadGeneration++;
        var gen = _loadGeneration;

        if (active == null)
        {
            _viewModel.Value = new LocalChangesViewModel.Placeholder("Open a repository to see local changes.");
            return;
        }

        _viewModel.Value = new LocalChangesViewModel.Placeholder("Loading…");

        var repo = active;
        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            LocalChangesViewModel result;
            try
            {
                var snap = service.GetLocalChanges(repo);
                result = snap.ErrorMessage != null
                    ? new LocalChangesViewModel.Placeholder(snap.ErrorMessage)
                    : new LocalChangesViewModel.Loaded(snap);
            }
            catch (Exception ex)
            {
                result = new LocalChangesViewModel.Placeholder(ex.Message);
            }

            dispatcher?.Post(() =>
            {
                if (gen != _loadGeneration) return;
                _viewModel.Value = result;
            });
        });
    }

    private void Render(LocalChangesViewModel vm)
    {
        switch (vm)
        {
            case LocalChangesViewModel.Placeholder p:
                ShowPlaceholder(p.Text);
                break;
            case LocalChangesViewModel.Loaded l:
                ShowSnapshot(l.Snapshot);
                break;
        }
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
