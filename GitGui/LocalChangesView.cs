using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

public sealed class LocalChangesView : MultiChildView
{
    private const int CommitBarPadding = 10;
    private const float CommitButtonWidth = 120f;
    private const float DescriptionMinHeight = 0f;
    private const float DescriptionMaxHeight = 240f;

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IMessageBus? _bus;
    private readonly SubscriptionGroup _subscriptions = new();

    private readonly State<LocalChangesViewModel> _viewModel = new(
        new LocalChangesViewModel.Placeholder("Open a repository to see local changes."));

    private readonly GenerationGuard _loadGen = new();

    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly MultiChildView _contentRow;
    private readonly DiffView _diffView;
    private readonly VerticalSplitContainer _snapshotContainer;
    private ColumnView _commitBarColumn = null!;
    private ErrorBar _opErrorBar = null!;
    private TextInputView _titleInput = null!;
    private GrowingDescriptionField _descriptionField = null!;
    private DialogButton _commitButton = null!;
    private CheckboxView _amendCheckbox = null!;
    private string _preAmendTitle = string.Empty;
    private string _preAmendDescription = string.Empty;

    // Cached so OnAmendToggled can re-derive the displayed staged list without
    // touching git. _stagedFromIndex is whatever GetLocalChanges last returned;
    // _headFiles is the diff of HEAD vs HEAD~1, populated only while amending.
    private IReadOnlyList<FileChange> _stagedFromIndex = Array.Empty<FileChange>();
    private IReadOnlyList<FileChange>? _headFiles;

    public LocalChangesView()
    {
        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            "No unstaged changes.",
            [
                (LucideIcons.ChevronRight, OnStageSelected),
                (LucideIcons.ChevronsRight, OnStageAll)
            ],
            path => Stage([path]),
            onEmptyAreaClicked: ClearAllSelections);
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            "No staged changes.",
            [
                (LucideIcons.ChevronsLeft, OnUnstageAll),
                (LucideIcons.ChevronLeft, OnUnstageSelected)
            ],
            path => Unstage([path]),
            onEmptyAreaClicked: ClearAllSelections);

        _placeholder = new TextView
        {
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _contentRow = BuildContentRow();
        _diffView = new DiffView();

        // Initial 1:2 split (files : diff). The container tracks the split as a fraction
        // of available height so window resizes scale both halves; the user can drag the
        // splitter to pick a different ratio, which then stays fractional across resizes.
        var splitterHovered = new State<bool>(false);
        var splitter = new RectView();
        splitter.BindBackgroundColor(splitterHovered,
            h => h ? CommitsPalette.DividerHoverBg : CommitsPalette.Border);

        _snapshotContainer = new VerticalSplitContainer(_contentRow, _diffView, splitter, bottomFraction: 2f / 3f);

        splitter.Behaviors.Add(new SplitterController(
            DragAxis.Y,
            _snapshotContainer.AdjustBottomFractionByPixels,
            h => splitterHovered.Value = h));

        _centerContainer = new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children = { _snapshotContainer },
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

    private MultiChildView BuildContentRow()
    {
        var divider = new RectView { PreferredWidth = 1, BackgroundColor = CommitsPalette.Border };

        // Custom layout instead of FlexRowView: with flex, each panel's content's natural
        // width (long file paths in unstaged, short placeholder in staged) leaks into the
        // distribution and the panels end up unequal. Here we measure only the center
        // divider and split the remainder strictly in half.
        return new TransferListRow(_unstagedPanel, divider, _stagedPanel);
    }

    private void ClearAllSelections()
    {
        _unstagedPanel.ClearSelection();
        _stagedPanel.ClearSelection();
    }

    private void OnStageAll() => Stage(_unstagedPanel.Files.Select(f => f.Path).ToList());
    private void OnStageSelected() => Stage(_unstagedPanel.SelectedPaths.ToList());
    private void OnUnstageSelected() => Unstage(_stagedPanel.SelectedPaths.ToList());
    private void OnUnstageAll() => Unstage(_stagedPanel.Files.Select(f => f.Path).ToList());

    private void Stage(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: true);

    private void Unstage(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;

        // While amending, the staged panel may include HEAD-only files (not in the
        // index) that the user wants to drop from the amended commit. Those need a
        // reset against HEAD~1; truly-staged files take the normal unstage path.
        if (_headFiles != null && _headFiles.Count > 0)
        {
            var stagedPaths = new HashSet<string>(_stagedFromIndex.Select(f => f.Path));
            List<string>? toUnstage = null;
            List<string>? toResetToParent = null;
            foreach (var p in paths)
            {
                if (stagedPaths.Contains(p))
                    (toUnstage ??= new List<string>()).Add(p);
                else
                    (toResetToParent ??= new List<string>()).Add(p);
            }

            if (toResetToParent != null)
            {
                RunUnstageWithReset(toUnstage ?? (IReadOnlyList<string>)Array.Empty<string>(), toResetToParent);
                return;
            }
        }

        RunIndexOp(paths, isStage: false);
    }

    private void RunIndexOp(IReadOnlyList<string> paths, bool isStage)
    {
        if (paths.Count == 0) return;
        if (_registry == null || _gitService == null) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        // Same generation guard as load: bump and capture so any in-flight worker that
        // resolves after a repo switch or another op doesn't clobber a fresher state.
        var gen = _loadGen.Bump();
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
                if (_loadGen.IsStale(gen)) return;
                ShowOpError(errorMsg);
                // Keep the prior snapshot rendered on failure — losing the list on every
                // transient error would erase the user's selection and context.
                if (newSnap != null)
                {
                    _viewModel.Value = new LocalChangesViewModel.Loaded(newSnap);
                    // The operated-on rows just moved sides; re-select them on the
                    // destination so the user keeps their place across a stage/unstage.
                    var destPanel = isStage ? _stagedPanel : _unstagedPanel;
                    destPanel.SetSelection(paths);
                }
            });
        });
    }

    private void RunUnstageWithReset(IReadOnlyList<string> toUnstage, IReadOnlyList<string> toResetToParent)
    {
        if (_registry == null || _gitService == null) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        var gen = _loadGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;

        Task.Run(() =>
        {
            LocalChangesSnapshot? newSnap = null;
            string? errorMsg = null;
            try
            {
                if (toUnstage.Count > 0) service.Unstage(repo, toUnstage);
                if (toResetToParent.Count > 0) service.ResetToParent(repo, toResetToParent);
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
                if (_loadGen.IsStale(gen)) return;
                ShowOpError(errorMsg);
                if (newSnap != null)
                {
                    _viewModel.Value = new LocalChangesViewModel.Loaded(newSnap);
                    // Both batches land in unstaged after the reset/unstage.
                    var combined = new List<string>(toUnstage.Count + toResetToParent.Count);
                    combined.AddRange(toUnstage);
                    combined.AddRange(toResetToParent);
                    _unstagedPanel.SetSelection(combined);
                }
            });
        });
    }

    private void ShowOpError(string? msg) => _opErrorBar.Message = msg;

    private View BuildCommitBar()
    {
        _titleInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
            PlaceholderText = "Commit title",
            PlaceholderTextColor = DialogPalette.RowTextMissing,
        };
        _titleInput.Behaviors.Add(new TextInputViewKbmController(_titleInput));
        _titleInput.TextChanged += UpdateCommitButtonEnabled;

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
            Children = { _titleInput },
        };

        _descriptionField = new GrowingDescriptionField(DescriptionMinHeight, DescriptionMaxHeight)
        {
            PlaceholderText = "Commit description",
        };

        _commitButton = new DialogButton("Commit", OnCommitClicked)
        {
            PreferredWidth = CommitButtonWidth,
            PreferredHeight = 28,
        };
        _commitButton.IsEnabled.Value = false;

        _amendCheckbox = new CheckboxView("Amend");
        _amendCheckbox.IsChecked.Changed += OnAmendToggled;

        var buttonRow = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.SpaceBetween,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { _amendCheckbox, _commitButton },
        };

        // Error bar is left out of the column until ShowOpError adds it — that way the
        // column gap doesn't reserve space for an absent banner.
        _commitBarColumn = new ColumnView
        {
            Gap = 8,
            Children = { titleBox, _descriptionField, buttonRow },
        };
        _opErrorBar = new ErrorBar(_commitBarColumn, insertAt: 0);

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
        if (_registry == null || _gitService == null) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        // UpdateCommitButtonEnabled gates the button on a non-empty title (and, unless
        // amending, at least one staged file), so reaching this point implies the
        // inputs are valid for the current mode.
        var title = _titleInput.Text.ToString().Trim();
        var description = _descriptionField.Text.ToString().Trim();
        // Standard git format: subject, blank line, body. Skip the blank line when there's
        // no body so the message is just the subject.
        var message = description.Length > 0 ? $"{title}\n\n{description}" : title;
        var amend = _amendCheckbox.IsChecked.Value;

        var gen = _loadGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            string? errorMsg = null;
            LocalChangesSnapshot? newSnap = null;
            try
            {
                errorMsg = service.Commit(repo, message, amend);
                if (errorMsg == null)
                {
                    var snap = service.GetLocalChanges(repo);
                    if (snap.ErrorMessage != null) errorMsg = snap.ErrorMessage;
                    else newSnap = snap;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            dispatcher?.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                ShowOpError(errorMsg);
                if (errorMsg != null) return;

                // Reset the pre-amend snapshot so toggling amend back off after the commit
                // doesn't restore stale text from a different commit's session.
                _preAmendTitle = string.Empty;
                _preAmendDescription = string.Empty;
                if (_amendCheckbox.IsChecked.Value)
                {
                    // Flipping IsChecked fires OnAmendToggled which clears the inputs
                    // (saved state was just emptied above).
                    _amendCheckbox.IsChecked.Value = false;
                }
                else
                {
                    _titleInput.Clear();
                    _descriptionField.Clear();
                }
                if (newSnap != null)
                    _viewModel.Value = new LocalChangesViewModel.Loaded(newSnap);
                bus?.Broadcast(new CommitCreatedMessage(repo.Id));
            });
        });
    }

    private void OnAmendToggled(bool checkedNow)
    {
        if (checkedNow)
        {
            _preAmendTitle = _titleInput.Text.ToString();
            _preAmendDescription = _descriptionField.Text.ToString();

            string title = string.Empty;
            string description = string.Empty;
            IReadOnlyList<FileChange> headFiles = Array.Empty<FileChange>();
            if (_registry != null && _gitService != null)
            {
                var repo = _registry.Active.Value;
                if (repo != null)
                {
                    var head = _gitService.GetHeadCommitMessage(repo);
                    if (head != null)
                    {
                        title = head.Title;
                        description = head.Description;
                    }
                    headFiles = _gitService.GetHeadCommitFiles(repo);
                }
            }

            _headFiles = headFiles;
            SetTitleText(title);
            SetDescriptionText(description);
        }
        else
        {
            SetTitleText(_preAmendTitle);
            SetDescriptionText(_preAmendDescription);
            _preAmendTitle = string.Empty;
            _preAmendDescription = string.Empty;
            _headFiles = null;
        }

        // Amend visibility flipped — re-render the staged panel so HEAD files appear
        // or disappear without waiting for the next snapshot reload.
        _stagedPanel.SetFiles(ComputeDisplayedStaged());
        UpdateCommitButtonEnabled();
    }

    private void SetTitleText(string text)
    {
        _titleInput.Clear();
        if (text.Length > 0) _titleInput.Enter(text.AsSpan());
    }

    private void SetDescriptionText(string text)
    {
        _descriptionField.SetText(text.AsSpan());
    }

    protected override void OnAttachedToContext(Context context)
    {
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _bus = context.Get<IMessageBus>();
        _subscriptions.Add(_viewModel.Subscribe(Render));
        _subscriptions.Add(_registry?.Active.Subscribe(_ => StartLoadForActiveRepo()));

        // Selection is exclusive across the two panels: once a row in one side is selected,
        // any selection on the other side is cleared. The "only clear when *becoming*
        // non-empty" guard means the cleared panel's own empty-transition doesn't bounce
        // back and wipe the panel that just took focus.
        _subscriptions.Add(_unstagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _stagedPanel.ClearSelection();
            UpdateDiffVisibility();
        }));
        _subscriptions.Add(_stagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _unstagedPanel.ClearSelection();
            UpdateDiffVisibility();
        }));
    }

    protected override void OnDetachedFromContext(Context context)
    {
        // Bump the generation so any in-flight worker's dispatcher.Post becomes a no-op.
        _loadGen.Bump();
        _subscriptions.Dispose();
        _registry = null;
        _gitService = null;
        _dispatcher = null;
        _bus = null;
    }

    private void StartLoadForActiveRepo()
    {
        if (_registry == null || _gitService == null) return;
        var active = _registry.Active.Value;

        var gen = _loadGen.Bump();
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
                if (_loadGen.IsStale(gen)) return;
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
        // No loaded snapshot → nothing committable. Stale _stagedPanel.Files from a prior
        // repo would otherwise leak through UpdateCommitButtonEnabled.
        _commitButton.IsEnabled.Value = false;
    }

    private void ShowSnapshot(LocalChangesSnapshot snap)
    {
        _stagedFromIndex = snap.Staged;
        _unstagedPanel.SetFiles(snap.Unstaged);
        _stagedPanel.SetFiles(ComputeDisplayedStaged());
        // SetFiles clears both panels' selections, which fires the selection subscriptions
        // and drives UpdateDiffVisibility — so the diff item collapses on its own here.
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_snapshotContainer);
        UpdateCommitButtonEnabled();
    }

    // Outside amend mode the displayed staged list is just whatever the index says.
    // While amending we also surface HEAD's files (so the user can see — and optionally
    // remove — files that will otherwise carry over into the amended commit). For files
    // that appear in both lists, the index entry wins so the badge reflects the *current*
    // change rather than the previous-commit change.
    private IReadOnlyList<FileChange> ComputeDisplayedStaged()
    {
        if (_headFiles == null || _headFiles.Count == 0)
            return _stagedFromIndex;

        var seen = new HashSet<string>(_stagedFromIndex.Select(f => f.Path));
        var merged = new List<FileChange>(_stagedFromIndex.Count + _headFiles.Count);
        merged.AddRange(_stagedFromIndex);
        foreach (var h in _headFiles)
        {
            if (seen.Add(h.Path))
                merged.Add(h);
        }
        merged.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        return merged;
    }

    private void UpdateCommitButtonEnabled()
    {
        var hasTitle = false;
        foreach (var ch in _titleInput.Text)
        {
            if (!char.IsWhiteSpace(ch)) { hasTitle = true; break; }
        }
        // Amend can be a message-only edit of the previous commit, so it doesn't need
        // anything staged; a regular commit does.
        var amend = _amendCheckbox.IsChecked.Value;
        _commitButton.IsEnabled.Value = hasTitle && (amend || _stagedPanel.Files.Count > 0);
    }

    private void UpdateDiffVisibility()
    {
        // Selections are mutually exclusive across the two panels (see OnAttachedToContext),
        // so the combined count is whichever panel currently holds anything.
        var unstaged = _unstagedPanel.SelectedPaths;
        var staged = _stagedPanel.SelectedPaths;
        var total = unstaged.Count + staged.Count;

        if (total == 1)
        {
            var side = unstaged.Count == 1 ? DiffSide.Unstaged : DiffSide.Staged;
            var path = unstaged.Count == 1 ? unstaged.First() : staged.First();
            _diffView.SetTarget(path, side);
            _snapshotContainer.BottomVisible = true;
        }
        else
        {
            _diffView.SetTarget(null, DiffSide.Unstaged);
            _snapshotContainer.BottomVisible = false;
        }
    }
}