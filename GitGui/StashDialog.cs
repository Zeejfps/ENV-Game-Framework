using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user clicks Stash in the actions toolbar. Lets the user name the
/// stash, optionally include untracked files (-u), and optionally keep the index
/// (--keep-index) so staged hunks stay around after stashing. Runs `git stash push`.
/// </summary>
public sealed class StashDialog : MultiChildView, IStashView
{
    private readonly Action _onClose;
    private readonly TextInputView _messageInput;
    private readonly CheckoutDialogKbmController _messageController;
    private readonly CheckboxView _includeUntrackedCheckbox;
    private readonly CheckboxView _keepStagedCheckbox;
    private readonly DialogButton _stashButton;
    private readonly TextView _errorView;

    public event Action? StashRequested;

    public StashDialog(Repo repo, Action onClose)
    {
        _onClose = onClose;

        var messageLabel = new TextView
        {
            Text = "Message",
            TextColor = DialogPalette.SectionHeaderText,
        };

        _messageInput = DialogFrame.TextInput();
        var messageBox = DialogFrame.WrapInput(_messageInput);

        _includeUntrackedCheckbox = new CheckboxView("Include untracked files")
        {
            PreferredHeight = 22,
        };
        _includeUntrackedCheckbox.IsChecked.Value = true;
        _keepStagedCheckbox = new CheckboxView("Keep staged changes in index")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _stashButton = new DialogButton("Stash", RaiseStashRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Stash changes", onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                messageLabel,
                messageBox,
                _includeUntrackedCheckbox,
                _keepStagedCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _stashButton),
            },
        }));

        // Same reason as CreateBranchDialog: text-input controllers consume clicks across
        // the view they're on, so attach to the input itself, not the outer dialog.
        _messageController = new CheckoutDialogKbmController(_messageInput, RaiseStashRequested, onClose);
        _messageInput.UseController(_ => _messageController);

        var request = new StashRequest(repo);
        this.UsePresenter(ctx => new StashPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseStashRequested() => StashRequested?.Invoke();

    public string Message => new(_messageInput.Text);
    public bool IncludeUntracked => _includeUntrackedCheckbox.IsChecked.Value;
    public bool KeepStaged => _keepStagedCheckbox.IsChecked.Value;
    public bool StashEnabled
    {
        set => _stashButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }
    public event Action MessageChanged
    {
        add => _messageInput.TextChanged += value;
        remove => _messageInput.TextChanged -= value;
    }
    public void FocusMessage()
    {
        _messageController.BeginEditing();
    }
    public void Close() => _onClose();
}
