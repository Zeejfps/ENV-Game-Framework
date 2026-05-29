using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

public sealed class DeleteLocalBranchDialog : MultiChildView, IDeleteLocalBranchView
{
    private readonly Action _onClose;
    private readonly CheckboxView _forceCheckbox;
    private readonly CheckboxView? _deleteRemoteCheckbox;
    private readonly DialogButton _cancelButton;
    private readonly DialogButton _deleteButton;
    private readonly TextView _errorView;

    public event Action? DeleteRequested;

    public DeleteLocalBranchDialog(Repo repo, string branchName, Action onClose)
        : this(repo, branchName, upstreamRemote: null, upstreamBranch: null, onClose) { }

    public DeleteLocalBranchDialog(
        Repo repo,
        string branchName,
        string? upstreamRemote,
        string? upstreamBranch,
        Action onClose)
    {
        PreferredWidth = 460f;

        _onClose = onClose;

        var prompt = new TextView
        {
            Text = $"Delete local branch '{branchName}'?",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindThemedTextColor(s => s.DialogBody.BodyText);

        var hint = DialogFrame.Hint(
            "Unchecked: refuses if the branch isn't fully merged into its upstream or HEAD.",
            TextWrap.Wrap);

        _forceCheckbox = new CheckboxView("Delete even if not merged")
        {
            PreferredHeight = 22,
        };

        var hasUpstream = !string.IsNullOrEmpty(upstreamRemote) && !string.IsNullOrEmpty(upstreamBranch);
        if (hasUpstream)
        {
            _deleteRemoteCheckbox = new CheckboxView($"Also delete '{upstreamBranch}' on '{upstreamRemote}'")
            {
                PreferredHeight = 22,
            };
        }

        _errorView = DialogFrame.ErrorView();

        _cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _deleteButton = new DialogButton("Delete", RaiseDeleteRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        var content = new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                _forceCheckbox,
                hint,
            },
        };
        if (_deleteRemoteCheckbox != null)
            content.Children.Add(_deleteRemoteCheckbox);
        content.Children.Add(_errorView);
        content.Children.Add(new MultiChildView { PreferredHeight = 4 });
        content.Children.Add(DialogFrame.ButtonsRow(_cancelButton, _deleteButton));

        AddChildToSelf(DialogFrame.Build("Delete branch", onClose, content));

        this.UseController(_ => new DialogKbmController(RaiseDeleteRequested, onClose));

        var request = new DeleteLocalBranchRequest(repo, branchName, upstreamRemote, upstreamBranch);
        this.UsePresenter(ctx => new DeleteLocalBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool DeleteRemote => _deleteRemoteCheckbox?.IsChecked.Value ?? false;
    public bool DeleteEnabled
    {
        set => _deleteButton.IsEnabled.Value = value;
    }
    public bool CancelEnabled
    {
        set => _cancelButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }
    public bool IsBusy
    {
        set
        {
            _deleteButton.Icon = value ? LucideIcons.Loader : string.Empty;
            if (!value) _deleteButton.IconRotation = 0f;
        }
    }
    public float BusyRotation
    {
        set => _deleteButton.IconRotation = value;
    }

    private void RaiseDeleteRequested() => DeleteRequested?.Invoke();

    public void Close() => _onClose();
}
