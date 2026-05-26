using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for deleting a branch from a remote. Calls
/// `git push &lt;remote&gt; --delete &lt;branch&gt;` — a network operation that doesn't touch
/// local branches. The server may refuse for protected refs; that error is surfaced.
/// </summary>
public sealed class DeleteRemoteBranchDialog : MultiChildView, IDeleteRemoteBranchView
{
    private readonly Action _onClose;
    private readonly DialogButton _deleteButton;
    private readonly TextView _errorView;

    public event Action? DeleteRequested;

    public DeleteRemoteBranchDialog(Repo repo, string remoteName, string branchName, Action onClose)
    {
        PreferredWidth = 480f;

        _onClose = onClose;

        var prompt = new TextView
        {
            Text = $"Delete '{branchName}' from remote '{remoteName}'?",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var hint = new TextView
        {
            Text = "This is a network operation. Your local branches are not affected.",
            TextWrap = TextWrap.Wrap,
        };
        hint.BindTextColorFromTheme(t => t.Dialog.RowTextMissing);

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _deleteButton = new DialogButton("Delete", RaiseDeleteRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Delete remote branch", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                hint,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _deleteButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(RaiseDeleteRequested, onClose));

        var request = new DeleteRemoteBranchRequest(repo, remoteName, branchName);
        this.UsePresenter(ctx => new DeleteRemoteBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool DeleteEnabled
    {
        set => _deleteButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    private void RaiseDeleteRequested() => DeleteRequested?.Invoke();

    public void Close() => _onClose();
}
