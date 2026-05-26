using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ForcePushDialog : MultiChildView, IForcePushView
{
    private readonly Action _onClose;
    private readonly DialogButton _forcePushButton;
    private readonly TextView _errorView;

    public event Action? ForcePushRequested;

    public ForcePushDialog(Repo repo, string branchName, int ahead, int behind, Action onClose)
    {
        PreferredWidth = 520f;
        PreferredHeight = 260f;

        _onClose = onClose;

        var displayBranch = string.IsNullOrEmpty(branchName) ? "this branch" : $"'{branchName}'";
        var prompt = new TextView
        {
            Text = $"{displayBranch} has diverged from its upstream — {ahead} ahead, {behind} behind. "
                 + "A regular push will be rejected. Force-push (with lease) will overwrite the remote "
                 + "branch with your local history; any commits on the remote that you haven't fetched "
                 + "will be lost. The lease refuses the push if the remote moved since your last fetch.",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindTextColorFromTheme(t => t.Dialog.BodyText);

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _forcePushButton = new DialogButton("Force push", RaiseForcePushRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Force push?", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = prompt },
                _errorView,
                DialogFrame.ButtonsRow(cancelButton, _forcePushButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(RaiseForcePushRequested, onClose));

        this.UsePresenter(ctx => new ForcePushPresenter(
            this, repo,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool ForcePushEnabled
    {
        set => _forcePushButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public void Close() => _onClose();

    private void RaiseForcePushRequested() => ForcePushRequested?.Invoke();
}
