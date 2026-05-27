using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user double-clicks a remote branch that has no matching local
/// branch. Lets them pick the local branch name and whether to set up tracking, then
/// runs `git checkout -b <local> [--track|--no-track] <remote>/<branch>`.
/// </summary>
public sealed class CheckoutBranchDialog : MultiChildView, ICheckoutBranchView
{
    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckoutDialogKbmController _nameController;
    private readonly CheckboxView _trackCheckbox;
    private readonly DialogButton _checkoutButton;

    public event Action? CheckoutRequested;

    public CheckoutBranchDialog(
        Repo repo,
        string remoteName,
        string remoteBranchName,
        string suggestedLocalName,
        Action onClose)
    {
        PreferredWidth = 420f;
        PreferredHeight = 280f;

        _onClose = onClose;

        var subtitle = new TextView { Text = $"Create a local branch from {remoteName}/{remoteBranchName}" };
        subtitle.BindThemedTextColor(s => s.DialogBody.BodyText);

        var nameLabel = new TextView { Text = "Local branch name" };
        nameLabel.BindThemedTextColor(s => s.DialogBody.SectionHeaderText);

        _nameInput = DialogFrame.TextInput();
        var nameBox = DialogFrame.WrapInput(_nameInput);

        _trackCheckbox = new CheckboxView("Track this remote branch")
        {
            PreferredHeight = 22,
            IsChecked =
            {
                Value = true
            }
        };

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _checkoutButton = new DialogButton("Checkout", RaiseCheckoutRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Checkout branch", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                subtitle,
                nameLabel,
                nameBox,
                _trackCheckbox,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _checkoutButton),
            },
        }));

        // Controller goes on the INPUT's Behaviors, not the outer dialog's:
        // BaseTextInputKbmController.OnMouseButtonStateChanged consumes left-press events
        // anywhere inside the view it's attached to, so putting it on the dialog would
        // swallow clicks meant for the Cancel/Checkout buttons.
        _nameController = new CheckoutDialogKbmController(_nameInput, RaiseCheckoutRequested, onClose);
        _nameInput.UseController(_ => _nameController);

        var request = new CheckoutRequest(repo, remoteName, remoteBranchName, suggestedLocalName);
        this.UsePresenter(ctx => new CheckoutBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseCheckoutRequested() => CheckoutRequested?.Invoke();

    public string Name => new(_nameInput.Text);
    public bool Track => _trackCheckbox.IsChecked.Value;
    public bool CheckoutEnabled
    {
        set => _checkoutButton.IsEnabled.Value = value;
    }
    public event Action NameChanged
    {
        add => _nameInput.TextChanged += value;
        remove => _nameInput.TextChanged -= value;
    }
    public void FocusName(string initialText)
    {
        // Must run after the input is attached to a context — doing it earlier produced an
        // empty-looking field (buffer wrote OK, but StartEditing/StealFocus hadn't run yet
        // so caret/selection visuals were stale and typing didn't engage).
        if (initialText.Length > 0)
            _nameInput.Enter(initialText);
        _nameInput.SelectAll();
        _nameController.BeginEditing();
    }
    public void Close() => _onClose();
}