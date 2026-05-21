using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Banner shown above the main content area while the repo is mid-operation
/// (merge / rebase / cherry-pick / revert / bisect / am) or has unmerged paths from a
/// stash-apply conflict. Same warning palette as <see cref="ErrorBar"/>; hides by removing
/// itself from <see cref="_container"/> so the parent's gap collapses, shows by
/// re-inserting at <see cref="_insertAt"/>.
///
/// Exposes <see cref="AbortRequested"/> so the presenter can route the click to the
/// confirmation dialog without coupling the banner to the bus directly. <see cref="CurrentState"/>
/// stays in sync with whatever the presenter set last so the dialog message can carry it.
/// </summary>
internal sealed class OperationStateBanner
{
    public RectView View { get; }
    public RepoOperationState CurrentState { get; private set; } = RepoOperationState.None;

    public event Action? AbortRequested;
    public event Action? ContinueRequested;

    private readonly TextView _text;
    private readonly ActionButton _abortButton;
    private readonly ActionButton _continueButton;
    private readonly TextView _spinnerIcon;
    private readonly FlexItem _textItem;
    private readonly FlexRowView _row;
    private readonly MultiChildView _container;
    private readonly int _insertAt;
    private bool _isBusy;

    public OperationStateBanner(MultiChildView container, int insertAt = -1)
    {
        _container = container;
        _insertAt = insertAt;

        _text = new TextView
        {
            TextColor = CommitsPalette.WarningText,
            VerticalTextAlignment = TextAlignment.Center,
            TextWrap = TextWrap.Wrap,
        };

        _continueButton = new ActionButton(
            LucideIcons.ChevronsRight,
            () => ContinueRequested?.Invoke(),
            tooltip: "Continue",
            backgroundColor: 0xFF4E8B3D);

        _abortButton = new ActionButton(
            LucideIcons.X,
            () => AbortRequested?.Invoke(),
            tooltip: "Abort",
            backgroundColor: 0xFFB3514B);

        _spinnerIcon = new TextView
        {
            Text = LucideIcons.Loader,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 16,
            TextColor = CommitsPalette.WarningText,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 20,
        };

        _textItem = new FlexItem { Grow = 1, Child = _text };

        _row = new FlexRowView
        {
            Gap = 4,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { _textItem, _abortButton },
        };

        View = new RectView
        {
            BackgroundColor = CommitsPalette.WarningBg,
            BorderColor = new BorderColorStyle { Bottom = CommitsPalette.WarningBorder },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle
            {
                Left = 12,
                Right = 12,
                Top = 6,
                Bottom = 6,
            },
            Children = { _row },
        };
    }

    public RepoOperationState State
    {
        set
        {
            CurrentState = value;
            if (value == RepoOperationState.None)
            {
                _isBusy = false;
                _spinnerIcon.Rotation = 0f;
                _container.Children.Remove(View);
                return;
            }
            Render();
            if (!_container.Children.Contains(View))
            {
                if (_insertAt < 0) _container.Children.Add(View);
                else _container.Children.Insert(_insertAt, View);
            }
        }
    }

    // Drives the in-flight visual: text swaps to "Continuing rebase…", both action buttons
    // detach, and a spinner icon takes their slot. Set back to false on completion or error
    // so the banner restores to the normal warning copy + buttons.
    public bool IsBusy
    {
        set
        {
            _isBusy = value;
            if (!value) _spinnerIcon.Rotation = 0f;
            if (CurrentState != RepoOperationState.None) Render();
        }
    }

    public float BusyRotation
    {
        set => _spinnerIcon.Rotation = value;
    }

    private void Render()
    {
        _row.Children.Clear();
        _row.Children.Add(_textItem);
        if (_isBusy)
        {
            _text.Text = BusyMessageFor(CurrentState);
            _row.Children.Add(_spinnerIcon);
            return;
        }
        _text.Text = MessageFor(CurrentState);
        if (SupportsContinue(CurrentState)) _row.Children.Add(_continueButton);
        _row.Children.Add(_abortButton);
    }

    private static bool SupportsContinue(RepoOperationState state) => state switch
    {
        RepoOperationState.Merge => true,
        RepoOperationState.Rebase => true,
        RepoOperationState.CherryPick => true,
        RepoOperationState.Revert => true,
        RepoOperationState.ApplyMailbox => true,
        _ => false,
    };

    private static string BusyMessageFor(RepoOperationState state) => state switch
    {
        RepoOperationState.Merge => "Continuing merge…",
        RepoOperationState.Rebase => "Continuing rebase…",
        RepoOperationState.CherryPick => "Continuing cherry-pick…",
        RepoOperationState.Revert => "Continuing revert…",
        RepoOperationState.ApplyMailbox => "Continuing patch apply…",
        _ => "Working…",
    };

    private static string MessageFor(RepoOperationState state) => state switch
    {
        RepoOperationState.Merge =>
            "Merge in progress — working directory contains unmerged files. Resolve conflicts and commit, or abort.",
        RepoOperationState.Rebase =>
            "Rebase in progress — working directory contains unmerged files. Resolve conflicts and continue, or abort.",
        RepoOperationState.CherryPick =>
            "Cherry-pick in progress — working directory contains unmerged files. Resolve conflicts and commit, or abort.",
        RepoOperationState.Revert =>
            "Revert in progress — working directory contains unmerged files. Resolve conflicts and commit, or abort.",
        RepoOperationState.ApplyMailbox =>
            "Patch apply in progress — working directory contains unmerged files. Resolve conflicts and continue, or abort.",
        RepoOperationState.Bisect =>
            "Bisect in progress. Use the terminal to mark commits good/bad, or reset.",
        RepoOperationState.UnmergedPaths =>
            "Working directory contains unresolved conflicts. Resolve them and stage the files to clear this state, or reset.",
        _ => string.Empty,
    };
}
