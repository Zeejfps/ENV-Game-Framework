using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown from a primary RepoRow's "New worktree…" menu. Collects the three
/// fields `git worktree add` needs (path, start point, optional new branch name) plus
/// a force toggle for re-using an existing dirty path.
/// </summary>
internal sealed class CreateWorktreeDialog : MultiChildView, IBind<CreateWorktreeDialogViewModel>
{
    private readonly Action _onClose;
    private readonly TextInputView _pathInput;
    private readonly CheckoutDialogKbmController _pathController;
    private readonly TextInputView _startPointInput;
    private readonly TextInputView _branchInput;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _createButton;
    private readonly TextView _errorView;
    private CreateWorktreeDialogViewModel? _vm;

    public CreateWorktreeDialog(Repo primary, Action onClose)
    {
        _onClose = onClose;

        var pathLabel = DialogFrame.Label("Worktree path");

        _pathInput = DialogFrame.TextInput();

        var browseButton = new DialogButton("Browse…", PickPath)
        {
            PreferredHeight = 28,
            PreferredWidth = 80,
        };

        var pathRow = new FlexRowView
        {
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new FlexItem { Grow = 1, Child = DialogFrame.WrapInput(_pathInput) },
                browseButton,
            },
        };

        var startPointLabel = DialogFrame.Label("Start point");

        _startPointInput = DialogFrame.TextInput();
        var startPointBox = DialogFrame.WrapInput(_startPointInput);

        var startPointHint = DialogFrame.Hint("Branch, tag, or commit SHA.");

        var branchLabel = DialogFrame.Label("New branch (optional)");

        _branchInput = DialogFrame.TextInput();
        var branchBox = DialogFrame.WrapInput(_branchInput);

        var branchHint = DialogFrame.Hint("Leave blank to check out the start point as-is.");

        _forceCheckbox = new CheckboxView("Force (allow non-empty path or re-used branch)")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _createButton = new DialogButton("Create") { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("New worktree", onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                pathLabel,
                pathRow,
                startPointLabel,
                startPointBox,
                startPointHint,
                branchLabel,
                branchBox,
                branchHint,
                _forceCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _createButton),
            },
        }));

        _pathController = new CheckoutDialogKbmController(_pathInput, _createButton.Command, onClose);
        _pathInput.UseController(_ => _pathController);
        _startPointInput.UseController(_ => new CheckoutDialogKbmController(_startPointInput, _createButton.Command, onClose));
        _branchInput.UseController(_ => new CheckoutDialogKbmController(_branchInput, _createButton.Command, onClose));

        var request = new CreateWorktreeRequest(primary);
        this.UseViewModel(
            ctx => new CreateWorktreeDialogViewModel(
                request,
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>()),
            Bind);
    }

    public void Bind(CreateWorktreeDialogViewModel vm)
    {
        _vm = vm;
        vm.CloseRequested += _onClose;

        _pathInput.BindTwoWay(vm.Path);
        _startPointInput.BindTwoWay(vm.StartPoint);
        _branchInput.BindTwoWay(vm.NewBranchName);
        _forceCheckbox.IsChecked.BindTwoWay(vm.Force);
        _createButton.BindCommand(vm.Create);
        _errorView.BindText(vm.Create.Error, s => s ?? string.Empty);

        _pathController.BeginEditing();
    }

    private void PickPath()
    {
        var shell = Context?.Get<IPlatformShell>();
        var picked = shell?.PickFolder("Select worktree location");
        if (!string.IsNullOrEmpty(picked) && _vm != null)
        {
            _vm.Path.Value = picked;
        }
    }
}
