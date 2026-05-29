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
internal sealed class AddSubmoduleDialog : MultiChildView, IBind<AddSubmoduleDialogViewModel>
{
    private readonly Action _onClose;
    private readonly TextInputView _urlInput;
    private readonly CheckoutDialogKbmController _urlController;
    private readonly TextInputView _pathInput;
    private readonly TextInputView _branchInput;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _addButton;
    private readonly TextView _errorView;
    private AddSubmoduleDialogViewModel? _vm;

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
        _addButton = new DialogButton("Add") { PreferredHeight = DialogFrame.DefaultButtonHeight };

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

        _urlController = new CheckoutDialogKbmController(_urlInput, Submit, onClose);
        _urlInput.UseController(_ => _urlController);
        _pathInput.UseController(_ => new CheckoutDialogKbmController(_pathInput, Submit, onClose));
        _branchInput.UseController(_ => new CheckoutDialogKbmController(_branchInput, Submit, onClose));

        var request = new AddSubmoduleViewRequest(primary);
        this.UseViewModel(
            ctx => new AddSubmoduleDialogViewModel(
                request,
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>()),
            Bind);
    }

    public void Bind(AddSubmoduleDialogViewModel vm)
    {
        _vm = vm;
        vm.CloseRequested += _onClose;

        _urlInput.BindTwoWay(vm.Url);
        _pathInput.BindTwoWay(vm.Path);
        _branchInput.BindTwoWay(vm.Branch);
        _forceCheckbox.IsChecked.BindTwoWay(vm.Force);
        _addButton.BindCommand(vm.Add);
        _errorView.BindText(vm.Add.Error, s => s ?? string.Empty);

        _urlController.BeginEditing();
    }

    private void Submit() => _vm?.Add.Execute();
}
