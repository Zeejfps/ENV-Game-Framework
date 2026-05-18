using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

public sealed class LocalChangesView : MultiChildView, ILocalChangesView
{
    private const int CommitBarPadding = 10;
    private const float CommitButtonWidth = 120f;
    private const float DescriptionMinHeight = 0f;
    private const float DescriptionMaxHeight = 240f;

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

    public event Action<IReadOnlyList<string>>? StageRequested;
    public event Action<IReadOnlyList<string>>? UnstageRequested;
    public event Action? TitleChanged;
    public event Action? AmendToggled;
    public event Action? CommitClicked;

    public LocalChangesView()
    {
        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            "No unstaged changes.",
            [
                (LucideIcons.ChevronRight, OnStageSelected),
                (LucideIcons.ChevronsRight, OnStageAll)
            ],
            path => StageRequested?.Invoke([path]),
            onEmptyAreaClicked: ClearAllSelections);
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            "No staged changes.",
            [
                (LucideIcons.ChevronsLeft, OnUnstageAll),
                (LucideIcons.ChevronLeft, OnUnstageSelected)
            ],
            path => UnstageRequested?.Invoke([path]),
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

        // Selection is exclusive across the two panels: once a row in one side is selected,
        // any selection on the other side is cleared. The "only clear when *becoming*
        // non-empty" guard means the cleared panel's own empty-transition doesn't bounce
        // back and wipe the panel that just took focus.
        _unstagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _stagedPanel.ClearSelection();
            UpdateDiffVisibility();
        });
        _stagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _unstagedPanel.ClearSelection();
            UpdateDiffVisibility();
        });

        this.UsePresenter(ctx => new LocalChangesPresenter(
            this,
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
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

    private void OnStageAll() => StageRequested?.Invoke(_unstagedPanel.Files.Select(f => f.Path).ToList());
    private void OnStageSelected() => StageRequested?.Invoke(_unstagedPanel.SelectedPaths.ToList());
    private void OnUnstageSelected() => UnstageRequested?.Invoke(_stagedPanel.SelectedPaths.ToList());
    private void OnUnstageAll() => UnstageRequested?.Invoke(_stagedPanel.Files.Select(f => f.Path).ToList());

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
        _titleInput.TextChanged += () => TitleChanged?.Invoke();

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

        _commitButton = new DialogButton("Commit", () => CommitClicked?.Invoke())
        {
            PreferredWidth = CommitButtonWidth,
            PreferredHeight = 28,
        };

        _amendCheckbox = new CheckboxView("Amend");
        _amendCheckbox.IsChecked.Changed += _ => AmendToggled?.Invoke();

        var buttonRow = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.SpaceBetween,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { _amendCheckbox, _commitButton },
        };

        // Error bar is left out of the column until OpError adds it — that way the
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

    public void ShowPlaceholder(string text)
    {
        _placeholder.Text = text;
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_placeholder);
    }

    public void ShowSnapshot(IReadOnlyList<FileChange> unstaged, IReadOnlyList<FileChange> staged)
    {
        // Re-attach the snapshot container BEFORE populating panels. After a checkout the
        // load transitions via Placeholder("Loading…"), which swaps `_placeholder` into
        // `_centerContainer` and leaves `_snapshotContainer` (and the panels inside it) as
        // a detached subtree. Appending new row views to a detached parent leaves them
        // un-attached, and the rows render blank when the container later returns — even
        // though the header text (a string mutation on a still-live TextView) updates fine.
        // Attaching first means every SelectableFileRowView SetFiles adds is born into an
        // attached parent.
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_snapshotContainer);
        _unstagedPanel.SetFiles(unstaged);
        _stagedPanel.SetFiles(staged);
        // SetFiles clears both panels' selections, which fires the selection subscriptions
        // and drives UpdateDiffVisibility — so the diff item collapses on its own here.
    }

    public void SetStagedFiles(IReadOnlyList<FileChange> files) => _stagedPanel.SetFiles(files);

    public string TitleText
    {
        get => _titleInput.Text.ToString();
        set
        {
            _titleInput.Clear();
            if (value.Length > 0) _titleInput.Enter(value.AsSpan());
        }
    }

    public string DescriptionText
    {
        get => _descriptionField.Text.ToString();
        set => _descriptionField.SetText(value.AsSpan());
    }

    public bool AmendChecked
    {
        get => _amendCheckbox.IsChecked.Value;
        set => _amendCheckbox.IsChecked.Value = value;
    }

    public bool CommitEnabled { set => _commitButton.IsEnabled.Value = value; }
    public string? OpError { set => _opErrorBar.Message = value; }

    public void SelectUnstaged(IReadOnlyList<string> paths) => _unstagedPanel.SetSelection(paths);
    public void SelectStaged(IReadOnlyList<string> paths) => _stagedPanel.SetSelection(paths);

    private void UpdateDiffVisibility()
    {
        // Selections are mutually exclusive across the two panels (see constructor),
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
