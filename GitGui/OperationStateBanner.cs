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

    private readonly TextView _text;
    private readonly DialogButton _abortButton;
    private readonly MultiChildView _container;
    private readonly int _insertAt;

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

        _abortButton = new DialogButton("Abort", () => AbortRequested?.Invoke())
        {
            PreferredHeight = 24,
            PreferredWidth = 90,
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
            Children =
            {
                new FlexRowView
                {
                    Gap = 12,
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children =
                    {
                        new FlexItem { Grow = 1, Child = _text },
                        _abortButton,
                    },
                },
            },
        };
    }

    public RepoOperationState State
    {
        set
        {
            CurrentState = value;
            if (value == RepoOperationState.None)
            {
                _container.Children.Remove(View);
                return;
            }
            _text.Text = MessageFor(value);
            _abortButton.Label = ButtonLabelFor(value);
            if (!_container.Children.Contains(View))
            {
                if (_insertAt < 0) _container.Children.Add(View);
                else _container.Children.Insert(_insertAt, View);
            }
        }
    }

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

    // Label distinguishes destructive-revert ("Abort merge") from no-op-undo ("Reset bisect"
    // / "Reset"): the user shouldn't have to read the body to know what the button does.
    private static string ButtonLabelFor(RepoOperationState state) => state switch
    {
        RepoOperationState.Bisect => "Reset",
        RepoOperationState.UnmergedPaths => "Reset",
        _ => "Abort",
    };
}
