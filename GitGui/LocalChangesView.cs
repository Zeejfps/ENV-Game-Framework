using ZGF.Gui;
using ZGF.Gui.Bindings;
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
    private const int CommitBarPadding = 10;
    private const float CommitButtonWidth = 120f;
    private const float DescriptionMinHeight = 60f;
    private const float DescriptionMaxHeight = 240f;

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IDisposable? _activeSubscription;
    private IDisposable? _vmSubscription;
    private IDisposable? _unstagedSelectionSub;
    private IDisposable? _stagedSelectionSub;

    private readonly State<LocalChangesViewModel> _viewModel = new(
        new LocalChangesViewModel.Placeholder("Open a repository to see local changes."));

    private int _loadGeneration;

    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly View _contentRow;
    private ColumnView _commitBarColumn = null!;
    private RectView _opErrorBar = null!;
    private TextView _opErrorText = null!;

    public LocalChangesView()
    {
        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            "No unstaged changes.",
            new (string, Action)[]
            {
                (LucideIcons.ChevronRight, OnStageSelected),
                (LucideIcons.ChevronsRight, OnStageAll),
            },
            path => Stage(new[] { path }));
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            "No staged changes.",
            new (string, Action)[]
            {
                (LucideIcons.ChevronsLeft, OnUnstageAll),
                (LucideIcons.ChevronLeft, OnUnstageSelected),
            },
            path => Unstage(new[] { path }));

        _placeholder = new TextView
        {
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _contentRow = BuildContentRow();

        _centerContainer = new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children = { _contentRow },
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children =
            {
                new BorderLayoutView
                {
                    Center = _centerContainer,
                    South = BuildCommitBar(),
                },
            },
        });
    }

    private View BuildContentRow()
    {
        var divider = new RectView { PreferredWidth = 1, BackgroundColor = CommitsPalette.Border };

        // Custom layout instead of FlexRowView: with flex, each panel's content's natural
        // width (long file paths in unstaged, short placeholder in staged) leaks into the
        // distribution and the panels end up unequal. Here we measure only the center
        // divider and split the remainder strictly in half.
        return new TransferListRow(_unstagedPanel, divider, _stagedPanel);
    }

    private void OnStageAll() => Stage(_unstagedPanel.Files.Select(f => f.Path).ToList());
    private void OnStageSelected() => Stage(_unstagedPanel.SelectedPaths.ToList());
    private void OnUnstageSelected() => Unstage(_stagedPanel.SelectedPaths.ToList());
    private void OnUnstageAll() => Unstage(_stagedPanel.Files.Select(f => f.Path).ToList());

    private void Stage(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: true);
    private void Unstage(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: false);

    private void RunIndexOp(IReadOnlyList<string> paths, bool isStage)
    {
        if (paths.Count == 0) return;
        if (_registry == null || _gitService == null) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        // Same generation guard as load: bump and capture so any in-flight worker that
        // resolves after a repo switch or another op doesn't clobber a fresher state.
        _loadGeneration++;
        var gen = _loadGeneration;
        var service = _gitService;
        var dispatcher = _dispatcher;

        Task.Run(() =>
        {
            LocalChangesSnapshot? newSnap = null;
            string? errorMsg = null;
            try
            {
                if (isStage) service.Stage(repo, paths);
                else service.Unstage(repo, paths);
                var snap = service.GetLocalChanges(repo);
                if (snap.ErrorMessage != null) errorMsg = snap.ErrorMessage;
                else newSnap = snap;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            dispatcher?.Post(() =>
            {
                if (gen != _loadGeneration) return;
                ShowOpError(errorMsg);
                // Keep the prior snapshot rendered on failure — losing the list on every
                // transient error would erase the user's selection and context.
                if (newSnap != null)
                    _viewModel.Value = new LocalChangesViewModel.Loaded(newSnap);
            });
        });
    }

    private void ShowOpError(string? msg)
    {
        if (msg == null)
        {
            if (_commitBarColumn.Children.Contains(_opErrorBar))
                _commitBarColumn.Children.Remove(_opErrorBar);
            return;
        }

        _opErrorText.Text = msg;
        if (!_commitBarColumn.Children.Contains(_opErrorBar))
            _commitBarColumn.Children.Insert(0, _opErrorBar);
    }

    private View BuildCommitBar()
    {
        var titleInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
        };
        titleInput.Behaviors.Add(new TextInputViewKbmController(titleInput));

        // No PreferredHeight — let the box size to one line of text plus padding/border.
        // The input itself reports MeasureHeight = lineHeight (single line, NoWrap), and the
        // RectView adds its own padding+border on top.
        var titleBox = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            Children = { titleInput },
        };

        var descriptionField = new GrowingDescriptionField(DescriptionMinHeight, DescriptionMaxHeight);

        var commitButton = new DialogButton("Commit", OnCommitClicked)
        {
            PreferredWidth = CommitButtonWidth,
            PreferredHeight = 28,
        };

        var buttonRow = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.End,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { commitButton },
        };

        _opErrorText = new TextView
        {
            TextColor = CommitsPalette.WarningText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _opErrorBar = new RectView
        {
            BackgroundColor = CommitsPalette.WarningBg,
            BorderColor = BorderColorStyle.All(CommitsPalette.WarningBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 8, Right = 8, Top = 4, Bottom = 4 },
            Children = { _opErrorText },
        };

        // Error bar is left out of the column until ShowOpError adds it — that way the
        // column gap doesn't reserve space for an absent banner.
        _commitBarColumn = new ColumnView
        {
            Gap = 8,
            Children = { titleBox, descriptionField, buttonRow },
        };

        return new RectView
        {
            BackgroundColor = CommitsPalette.HeaderBg,
            BorderColor = new BorderColorStyle { Top = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Padding = new PaddingStyle
            {
                Left = CommitBarPadding,
                Right = CommitBarPadding,
                Top = CommitBarPadding,
                Bottom = CommitBarPadding,
            },
            Children = { _commitBarColumn },
        };
    }

    private void OnCommitClicked()
    {
        // UI-only for now.
    }

    protected override void OnAttachedToContext(Context context)
    {
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _vmSubscription = _viewModel.Subscribe(Render);
        if (_registry != null)
            _activeSubscription = _registry.Active.Subscribe(_ => StartLoadForActiveRepo());

        // Selection is exclusive across the two panels: once a row in one side is selected,
        // any selection on the other side is cleared. The "only clear when *becoming*
        // non-empty" guard means the cleared panel's own empty-transition doesn't bounce
        // back and wipe the panel that just took focus.
        _unstagedSelectionSub = _unstagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _stagedPanel.ClearSelection();
        });
        _stagedSelectionSub = _stagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _unstagedPanel.ClearSelection();
        });
    }

    protected override void OnDetachedFromContext(Context context)
    {
        // Bump the generation so any in-flight worker's dispatcher.Post becomes a no-op.
        _loadGeneration++;
        _activeSubscription?.Dispose();
        _activeSubscription = null;
        _vmSubscription?.Dispose();
        _vmSubscription = null;
        _unstagedSelectionSub?.Dispose();
        _unstagedSelectionSub = null;
        _stagedSelectionSub?.Dispose();
        _stagedSelectionSub = null;
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
        // Any error from a previous repo's op no longer applies once we switch/reload.
        ShowOpError(null);

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
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_placeholder);
    }

    private void ShowSnapshot(LocalChangesSnapshot snap)
    {
        _unstagedPanel.SetFiles(snap.Unstaged);
        _stagedPanel.SetFiles(snap.Staged);
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_contentRow);
    }
}

/// <summary>
/// Three-column row for the local-changes layout: left panel | fixed-width center |
/// right panel. The two side panels are guaranteed equal width — the center's measured
/// width is subtracted from the row's width and the remainder is split exactly in half.
/// </summary>
internal sealed class TransferListRow : MultiChildView
{
    private readonly View _left;
    private readonly View _center;
    private readonly View _right;

    public TransferListRow(View left, View center, View right)
    {
        _left = left;
        _center = center;
        _right = right;
        AddChildToSelf(left);
        AddChildToSelf(center);
        AddChildToSelf(right);
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        if (pos.Width <= 0f) return;

        var centerWidth = Math.Min(_center.MeasureWidth(), pos.Width);
        var sideWidth = Math.Max(0f, (pos.Width - centerWidth) / 2f);
        // Re-derive in case rounding pushed sideWidth lopsided.
        centerWidth = pos.Width - sideWidth * 2f;

        LayoutChild(_left, pos.Left, sideWidth, pos);
        LayoutChild(_center, pos.Left + sideWidth, centerWidth, pos);
        LayoutChild(_right, pos.Left + sideWidth + centerWidth, sideWidth, pos);
    }

    private static void LayoutChild(View child, float left, float width, in ZGF.Geometry.RectF parent)
    {
        child.LeftConstraint = left;
        child.BottomConstraint = parent.Bottom;
        child.MinWidthConstraint = width;
        child.MaxWidthConstraint = width;
        child.MaxHeightConstraint = parent.Height;
        child.LayoutSelf();
    }
}

internal sealed class LocalChangesHeaderActionButton : MultiChildView
{
    private const float ButtonSize = 22f;
    private const float IconSize = 13f;
    private const uint IconIdleColor = 0xFFB5B9C0;
    private const uint TransparentBg = 0x00000000u;

    public LocalChangesHeaderActionButton(string icon, Action onClick)
    {
        PreferredWidth = ButtonSize;
        PreferredHeight = ButtonSize;

        var isHovered = new State<bool>(false);

        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = IconSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindTextColor(isHovered, h => h ? 0xFFFFFFFFu : IconIdleColor);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(3),
            Children = { iconView },
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.ButtonHover : TransparentBg);
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(onClick, h => isHovered.Value = h));
    }
}

internal sealed class LocalChangesPanel : MultiChildView
{
    private const int ContentPadding = 10;
    private const int HeaderPadding = 4;
    private const int RowGap = 2;

    private readonly string _title;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly State<HashSet<string>> _selection = new(new HashSet<string>());
    private IReadOnlyList<FileChange> _files = Array.Empty<FileChange>();
    private string? _anchorPath;
    private Action<string>? _onRowActivated;

    public IReadable<HashSet<string>> Selection => _selection;
    public IReadOnlyCollection<string> SelectedPaths => _selection.Value;
    public IReadOnlyList<FileChange> Files => _files;

    public LocalChangesPanel(
        string title,
        string emptyText,
        IReadOnlyList<(string Icon, Action OnClick)>? headerActions = null,
        Action<string>? onRowActivated = null)
    {
        _title = title;
        _onRowActivated = onRowActivated;

        _headerText = new TextView
        {
            Text = FormatHeader(0),
            TextColor = FileChangesPalette.HeaderText,
        };
        _rows = new ColumnView { Gap = RowGap };
        _emptyPlaceholder = new TextView
        {
            Text = emptyText,
            TextColor = FileChangesPalette.HeaderText,
        };
        _rows.Children.Add(_emptyPlaceholder);

        View headerContent;
        if (headerActions is { Count: > 0 })
        {
            var actionRow = new FlexRowView
            {
                Gap = 2f,
                CrossAxisAlignment = CrossAxisAlignment.Center,
            };
            foreach (var (icon, onClick) in headerActions)
                actionRow.Children.Add(new LocalChangesHeaderActionButton(icon, onClick));

            headerContent = new FlexRowView
            {
                CrossAxisAlignment = CrossAxisAlignment.Center,
                Children =
                {
                    new FlexItem { Grow = 1, Child = _headerText },
                    actionRow,
                },
            };
        }
        else
        {
            headerContent = _headerText;
        }

        var headerBar = new RectView
        {
            BackgroundColor = FileChangesPalette.HeaderBg,
            BorderColor = new BorderColorStyle
            {
                Top = FileChangesPalette.HeaderBorder,
                Bottom = FileChangesPalette.HeaderBorder,
            },
            BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
            Padding = new PaddingStyle
            {
                Left = HeaderPadding,
                Right = HeaderPadding,
                Top = HeaderPadding,
                Bottom = HeaderPadding,
            },
            Children = { headerContent },
        };

        var paddedRows = new PaddingView
        {
            Padding = new PaddingStyle
            {
                Left = ContentPadding,
                Right = ContentPadding,
                Top = ContentPadding,
                Bottom = ContentPadding,
            },
            Children = { _rows },
        };

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(paddedRows);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _scrollBar = new VerticalScrollBarView
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
        _scrollBar.Thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        _scrollBar.Thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        _scrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        _scrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);
        _scrollBar.Behaviors.Add(new VerticalScrollBarViewController(_scrollBar));

        AddChildToSelf(new BorderLayoutView
        {
            North = headerBar,
            Center = _scrollPane,
            East = _scrollBar,
        });

        Behaviors.Add(new LocalChangesScrollSyncController(_scrollPane, _scrollBar));
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _files = files;
        _anchorPath = null;
        _headerText.Text = FormatHeader(files.Count);
        // The path set changed; drop any selection that no longer points at a real row.
        if (_selection.Value.Count > 0)
            _selection.Value = new HashSet<string>();
        _rows.Children.Clear();
        if (files.Count == 0)
        {
            _rows.Children.Add(_emptyPlaceholder);
        }
        else
        {
            foreach (var file in files)
                _rows.Children.Add(new SelectableFileRowView(file, _selection, HandleRowClick, _onRowActivated));
        }
        _scrollPane.ScrollToOrigin();
    }

    public void ClearSelection()
    {
        _anchorPath = null;
        if (_selection.Value.Count == 0) return;
        _selection.Value = new HashSet<string>();
    }

    private void HandleRowClick(string path, InputModifiers modifiers)
    {
        var shift = (modifiers & InputModifiers.Shift) != 0;
        // Cmd on macOS reports as Super; Ctrl on Windows/Linux as Control. Treat both
        // as the toggle-modifier so the panel feels right on every host.
        var toggle = (modifiers & (InputModifiers.Control | InputModifiers.Super)) != 0;

        if (shift && _anchorPath != null)
        {
            var anchorIdx = IndexOfPath(_anchorPath);
            var clickIdx = IndexOfPath(path);
            if (anchorIdx >= 0 && clickIdx >= 0)
            {
                var lo = Math.Min(anchorIdx, clickIdx);
                var hi = Math.Max(anchorIdx, clickIdx);
                var next = new HashSet<string>();
                for (var i = lo; i <= hi; i++)
                    next.Add(_files[i].Path);
                _selection.Value = next;
                // Anchor intentionally stays — extending the shift-range pivots around it.
                return;
            }
        }

        if (toggle)
        {
            var next = new HashSet<string>(_selection.Value);
            if (!next.Add(path)) next.Remove(path);
            _selection.Value = next;
            _anchorPath = path;
            return;
        }

        _selection.Value = new HashSet<string> { path };
        _anchorPath = path;
    }

    private int IndexOfPath(string path)
    {
        for (var i = 0; i < _files.Count; i++)
        {
            if (_files[i].Path == path) return i;
        }
        return -1;
    }

    private string FormatHeader(int count) => $"{_title} ({count})";
}

/// <summary>
/// Row in the local-changes lists. Same badge+path content as FileChangeRowView, but
/// clickable with reactive selection and hover backgrounds. The panel above handles
/// the modifier semantics (plain = single-select, Shift = range, Ctrl/Cmd = toggle);
/// this row just forwards the click + modifier state and renders the resulting state.
/// </summary>
internal sealed class SelectableFileRowView : MultiChildView
{
    private const float BadgeSize = 16f;
    private const int RowVerticalPadding = 2;
    private const int RowHorizontalPadding = 4;

    public SelectableFileRowView(
        FileChange file,
        IReadable<HashSet<string>> selection,
        Action<string, InputModifiers> onClick,
        Action<string>? onActivate = null)
    {
        var isHovered = new State<bool>(false);
        var path = file.Path;

        var badge = new RectView
        {
            PreferredWidth = BadgeSize,
            PreferredHeight = BadgeSize,
            BackgroundColor = FileChangesPalette.StatusColor(file.Status),
            BorderRadius = BorderRadiusStyle.All(3),
            Children =
            {
                new TextView
                {
                    Text = FileChangesPalette.StatusGlyph(file.Status),
                    TextColor = FileChangesPalette.BadgeText,
                    FontSize = 11f,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            },
        };

        var pathText = new TextView { Text = FileChangesPalette.FormatPath(file) };
        pathText.BindTextColor(() => selection.Value.Contains(path)
            ? DialogPalette.RowTextActive
            : DialogPalette.RowText);

        var content = new FlexRowView
        {
            Gap = 8f,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                badge,
                new FlexItem { Grow = 1, Child = pathText },
            },
        };

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = RowHorizontalPadding,
                Right = RowHorizontalPadding,
                Top = RowVerticalPadding,
                Bottom = RowVerticalPadding,
            },
            Children = { content },
        };
        background.BindBackgroundColor(() =>
            selection.Value.Contains(path) ? DialogPalette.RowActive
            : isHovered.Value ? DialogPalette.RowHover
            : DialogPalette.RowTransparent);

        AddChildToSelf(background);

        Behaviors.Add(new SelectableRowController(
            mods => onClick(path, mods),
            h => isHovered.Value = h,
            onActivate != null ? () => onActivate(path) : null));
    }
}

internal sealed class SelectableRowController : KeyboardMouseController
{
    private const int DoubleClickThresholdMs = 400;

    private readonly Action<InputModifiers> _onClick;
    private readonly Action? _onActivate;
    private readonly Action<bool> _onHoverChanged;

    private bool _hasLastClick;
    private int _lastClickTickMs;

    public SelectableRowController(
        Action<InputModifiers> onClick,
        Action<bool> onHoverChanged,
        Action? onActivate = null)
    {
        _onClick = onClick;
        _onActivate = onActivate;
        _onHoverChanged = onHoverChanged;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e) => _onHoverChanged(true);
    public override void OnMouseExit(ref MouseExitEvent e) => _onHoverChanged(false);

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            _onClick(e.Modifiers);

            if (_onActivate != null)
            {
                var now = Environment.TickCount;
                var isDouble = _hasLastClick
                    && unchecked(now - _lastClickTickMs) <= DoubleClickThresholdMs;
                if (isDouble)
                {
                    _onActivate();
                    // Reset so a third click in quick succession isn't also an activation.
                    _hasLastClick = false;
                }
                else
                {
                    _lastClickTickMs = now;
                    _hasLastClick = true;
                }
            }

            e.Consume();
        }
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

/// <summary>
/// A multi-line text input that auto-grows with its content between <c>min</c> and <c>max</c>.
/// Once content exceeds <c>max</c>, the field caps at that height and a vertical scroll bar
/// is shown so the rest is reachable by scrolling.
///
/// The desired height is recomputed in <see cref="OnLayoutChildren"/> (after the inner input
/// has been laid out — at which point its width is known and its <c>MeasureHeight</c> is
/// reliable) and stored as a <c>PreferredHeight</c>. The next layout pass reads that as the
/// desired size. This avoids the "measure before width is known" problem that would otherwise
/// make the field report a runaway height (every char treated as its own wrapped line).
/// </summary>
internal sealed class GrowingDescriptionField : MultiChildView
{
    private const float BoxBorderThickness = 1f;
    private const float BoxPaddingHorizontal = 6f;
    private const float BoxPaddingVertical = 4f;

    private readonly float _minHeight;
    private readonly float _maxHeight;

    private readonly TextInputView _input;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;

    public GrowingDescriptionField(float minHeight, float maxHeight)
    {
        _minHeight = minHeight;
        _maxHeight = maxHeight;

        _input = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextVerticalAlignment = TextAlignment.Start,
            TextWrap = TextWrap.Wrap,
        };
        _input.Behaviors.Add(new TextInputViewKbmController(_input) { IsMultiLine = true });

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(_input);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _scrollBar = new VerticalScrollBarView
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
        _scrollBar.Thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        _scrollBar.Thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        _scrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        _scrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);
        _scrollBar.Behaviors.Add(new VerticalScrollBarViewController(_scrollBar));

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All((int)BoxBorderThickness),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = (int)BoxPaddingHorizontal,
                Right = (int)BoxPaddingHorizontal,
                Top = (int)BoxPaddingVertical,
                Bottom = (int)BoxPaddingVertical,
            },
            Children =
            {
                new BorderLayoutView
                {
                    Center = _scrollPane,
                    East = _scrollBar,
                },
            },
        });

        Behaviors.Add(new LocalChangesScrollSyncController(_scrollPane, _scrollBar));

        // Start at the min size; the first OnLayoutChildren pass will refine this.
        PreferredHeight = _minHeight;
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();

        // Now that the input has been laid out, its MaxWidthConstraint reflects the actual
        // viewport width — so its MeasureHeight is reliable. Cache the clamped desired height
        // as PreferredHeight; the next layout pass will pick it up.
        var chrome = 2f * (BoxBorderThickness + BoxPaddingVertical);
        var contentHeight = _input.MeasureHeight();
        var desired = Math.Clamp(contentHeight + chrome, _minHeight, _maxHeight);
        if (Math.Abs(desired - (float)PreferredHeight) > 0.5f)
        {
            // Setting PreferredHeight via SetField marks us IsSelfDirty, so the next frame's
            // layout re-runs OnLayoutSelf with the new value.
            PreferredHeight = desired;
        }
    }
}
