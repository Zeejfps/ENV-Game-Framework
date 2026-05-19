using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class LocalChangesView : MultiChildView, ILocalChangesView
{
    private readonly LocalChangesContentView _content;
    private readonly CommitBarView _commitBar;

    public event Action<IReadOnlyList<string>>? StageRequested;
    public event Action<IReadOnlyList<string>>? UnstageRequested;
    public event Action? TitleChanged;
    public event Action? AmendToggled;
    public event Action? CommitClicked;

    public LocalChangesView()
    {
        _content = new LocalChangesContentView();
        _content.StageRequested += paths => StageRequested?.Invoke(paths);
        _content.UnstageRequested += paths => UnstageRequested?.Invoke(paths);

        _commitBar = new CommitBarView();
        _commitBar.TitleChanged += () => TitleChanged?.Invoke();
        _commitBar.AmendToggled += () => AmendToggled?.Invoke();
        _commitBar.CommitClicked += () => CommitClicked?.Invoke();

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children =
            {
                new BorderLayoutView
                {
                    Center = _content,
                    South = _commitBar,
                },
            },
        });

        this.UsePresenter(ctx => new LocalChangesPresenter(
            this,
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public void ShowPlaceholder(string text) => _content.ShowPlaceholder(text);
    public void ShowSnapshot(IReadOnlyList<FileChange> unstaged, IReadOnlyList<FileChange> staged)
        => _content.ShowSnapshot(unstaged, staged);
    public void SetStagedFiles(IReadOnlyList<FileChange> files) => _content.SetStagedFiles(files);
    public void SelectUnstaged(IReadOnlyList<string> paths) => _content.SelectUnstaged(paths);
    public void SelectStaged(IReadOnlyList<string> paths) => _content.SelectStaged(paths);

    public string TitleText
    {
        get => _commitBar.TitleText;
        set => _commitBar.TitleText = value;
    }

    public string DescriptionText
    {
        get => _commitBar.DescriptionText;
        set => _commitBar.DescriptionText = value;
    }

    public bool AmendChecked
    {
        get => _commitBar.AmendChecked;
        set => _commitBar.AmendChecked = value;
    }

    public bool CommitEnabled { set => _commitBar.CommitEnabled = value; }
    public string? OpError { set => _commitBar.OpError = value; }
}
