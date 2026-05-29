using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown from a primary RepoRow's "Add submodule…" menu. Collects the URL, path,
/// and optional tracked branch that `git submodule add` needs, plus a force toggle
/// for re-using a path that's been previously used.
/// </summary>
public sealed class AddSubmoduleDialog : MultiChildView, IAddSubmoduleView
{
    private readonly Action _onClose;
    private readonly TextInputView _urlInput;
    private readonly CheckoutDialogKbmController _urlController;
    private readonly TextInputView _pathInput;
    private readonly TextInputView _branchInput;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _addButton;
    private readonly TextView _errorView;

    public event Action? InputsChanged;
    public event Action? AddRequested;

    public AddSubmoduleDialog(Repo primary, Action onClose)
    {
        _onClose = onClose;

        _urlInput = DialogFrame.TextInput();
        _pathInput = DialogFrame.TextInput();
        _branchInput = DialogFrame.TextInput();

        _forceCheckbox = new CheckboxView("Force (allow paths previously used)")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _addButton = new DialogButton("Add", RaiseAddRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Add submodule", onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                DialogFrame.Label("Repository URL"),
                DialogFrame.WrapInput(_urlInput),
                DialogFrame.Label("Path inside parent"),
                DialogFrame.WrapInput(_pathInput),
                DialogFrame.Hint("Where to clone the submodule, relative to the parent root."),
                DialogFrame.Label("Track branch (optional)"),
                DialogFrame.WrapInput(_branchInput),
                DialogFrame.Hint("Leave blank to pin to the upstream HEAD at clone time."),
                _forceCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _addButton),
            },
        }));

        _urlController = new CheckoutDialogKbmController(_urlInput, RaiseAddRequested, onClose);
        _urlInput.UseController(_ => _urlController);
        _pathInput.UseController(_ => new CheckoutDialogKbmController(_pathInput, RaiseAddRequested, onClose));
        _branchInput.UseController(_ => new CheckoutDialogKbmController(_branchInput, RaiseAddRequested, onClose));

        _urlInput.TextChanged += RaiseInputsChanged;
        _pathInput.TextChanged += RaiseInputsChanged;

        var request = new AddSubmoduleViewRequest(primary);
        this.UsePresenter(ctx => new AddSubmodulePresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseAddRequested() => AddRequested?.Invoke();
    private void RaiseInputsChanged() => InputsChanged?.Invoke();

    public string Url => new(_urlInput.Text);
    public string Path => new(_pathInput.Text);
    public string Branch => new(_branchInput.Text);
    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool AddEnabled { set => _addButton.IsEnabled.Value = value; }
    public string? ErrorMessage { set => _errorView.Text = value ?? string.Empty; }
    public void FocusUrl() => _urlController.BeginEditing();
    public void Close() => _onClose();
}
