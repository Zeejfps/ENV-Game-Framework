using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user picks "Rename…" on a local branch row. Full branch path is
/// editable (slashes allowed) so cross-folder moves like feature/login → bugs/login work
/// the same as in `git branch -m`. The force checkbox switches the underlying call to -M,
/// allowing the rename to overwrite an existing branch of the new name.
/// </summary>
public sealed class RenameBranchDialog : MultiChildView, IRenameBranchView
{
    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckoutDialogKbmController _nameController;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _renameButton;
    private readonly TextView _errorView;
    private readonly string _currentName;

    public event Action? RenameRequested;

    public RenameBranchDialog(Repo repo, string currentName, Action onClose)
    {
        _onClose = onClose;
        _currentName = currentName;

        var subtitle = new TextView { Text = $"Renaming '{currentName}'" };
        subtitle.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var nameLabel = new TextView { Text = "New name" };
        nameLabel.BindTextColorFromTheme(t => t.Dialog.SectionHeaderText);

        _nameInput = DialogFrame.TextInput();
        var nameBox = DialogFrame.WrapInput(_nameInput);

        _forceCheckbox = new CheckboxView("Force rename even if target exists")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _renameButton = new DialogButton("Rename", RaiseRenameRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Rename branch", onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                subtitle,
                nameLabel,
                nameBox,
                _forceCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _renameButton),
            },
        }));

        _nameController = new CheckoutDialogKbmController(_nameInput, RaiseRenameRequested, onClose);
        _nameInput.UseController(_ => _nameController);

        var request = new RenameBranchRequest(repo, currentName);
        this.UsePresenter(ctx => new RenameBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseRenameRequested() => RenameRequested?.Invoke();

    public string Name => new(_nameInput.Text);
    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool RenameEnabled
    {
        set => _renameButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }
    public event Action NameChanged
    {
        add => _nameInput.TextChanged += value;
        remove => _nameInput.TextChanged -= value;
    }
    public void FocusName()
    {
        _nameInput.Enter(_currentName);
        _nameInput.SelectAll();
        _nameController.BeginEditing();
    }
    public void Close() => _onClose();
}
