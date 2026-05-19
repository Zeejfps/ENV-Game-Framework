using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Banner shown above the main content area while the repo is mid-operation
/// (merge / rebase / cherry-pick / revert / bisect / am). Same warning palette as
/// <see cref="ErrorBar"/>; hides by removing itself from <see cref="_container"/>
/// so the parent's gap collapses, shows by re-inserting at <see cref="_insertAt"/>.
/// </summary>
internal sealed class OperationStateBanner
{
    public RectView View { get; }
    private readonly TextView _text;
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
            Children = { _text },
        };
    }

    public RepoOperationState State
    {
        set
        {
            if (value == RepoOperationState.None)
            {
                _container.Children.Remove(View);
                return;
            }
            _text.Text = MessageFor(value);
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
            "Merge in progress — working directory contains unmerged files. Resolve conflicts and commit, or use the terminal to abort.",
        RepoOperationState.Rebase =>
            "Rebase in progress — working directory contains unmerged files. Resolve conflicts and continue, or use the terminal to abort.",
        RepoOperationState.CherryPick =>
            "Cherry-pick in progress — working directory contains unmerged files. Resolve conflicts and commit, or use the terminal to abort.",
        RepoOperationState.Revert =>
            "Revert in progress — working directory contains unmerged files. Resolve conflicts and commit, or use the terminal to abort.",
        RepoOperationState.ApplyMailbox =>
            "Patch apply in progress — working directory contains unmerged files. Resolve conflicts and continue, or use the terminal to abort.",
        RepoOperationState.Bisect =>
            "Bisect in progress. Use the terminal to mark commits good/bad or to reset.",
        RepoOperationState.UnmergedPaths =>
            "Working directory contains unresolved conflicts. Resolve them and stage the files to clear this state.",
        _ => string.Empty,
    };
}
