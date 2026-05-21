using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown from a primary RepoRow's "New worktree…" menu. Collects the three
/// fields `git worktree add` needs (path, start point, optional new branch name) plus
/// a force toggle for re-using an existing dirty path. The presenter shells out via
/// IGitService.AddWorktree.
/// </summary>
public sealed class CreateWorktreeDialog : MultiChildView, ICreateWorktreeView
{
    private readonly Action _onClose;
    private readonly TextInputView _pathInput;
    private readonly CheckoutDialogKbmController _pathController;
    private readonly TextInputView _startPointInput;
    private readonly TextInputView _branchInput;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _createButton;
    private readonly TextView _errorView;

    public event Action? InputsChanged;
    public event Action? CreateRequested;

    public CreateWorktreeDialog(Repo primary, Action onClose)
    {
        _onClose = onClose;

        var pathLabel = new TextView
        {
            Text = "Worktree path",
            TextColor = DialogPalette.SectionHeaderText,
        };

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

        var startPointLabel = new TextView
        {
            Text = "Start point",
            TextColor = DialogPalette.SectionHeaderText,
        };

        _startPointInput = DialogFrame.TextInput();
        _startPointInput.Enter("HEAD");
        var startPointBox = DialogFrame.WrapInput(_startPointInput);

        var startPointHint = new TextView
        {
            Text = "Branch, tag, or commit SHA.",
            TextColor = DialogPalette.RowTextMissing,
        };

        var branchLabel = new TextView
        {
            Text = "New branch (optional)",
            TextColor = DialogPalette.SectionHeaderText,
        };

        _branchInput = DialogFrame.TextInput();
        var branchBox = DialogFrame.WrapInput(_branchInput);

        var branchHint = new TextView
        {
            Text = "Leave blank to check out the start point as-is.",
            TextColor = DialogPalette.RowTextMissing,
        };

        _forceCheckbox = new CheckboxView("Force (allow non-empty path or re-used branch)")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _createButton = new DialogButton("Create", RaiseCreateRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

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

        _pathController = new CheckoutDialogKbmController(_pathInput, RaiseCreateRequested, onClose);
        _pathInput.UseController(_ => _pathController);
        _startPointInput.UseController(_ => new CheckoutDialogKbmController(_startPointInput, RaiseCreateRequested, onClose));
        _branchInput.UseController(_ => new CheckoutDialogKbmController(_branchInput, RaiseCreateRequested, onClose));

        _pathInput.TextChanged += RaiseInputsChanged;
        _startPointInput.TextChanged += RaiseInputsChanged;

        var request = new CreateWorktreeRequest(primary);
        this.UsePresenter(ctx => new CreateWorktreePresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseCreateRequested() => CreateRequested?.Invoke();
    private void RaiseInputsChanged() => InputsChanged?.Invoke();

    private void PickPath()
    {
        var shell = Context?.Get<IPlatformShell>();
        var picked = shell?.PickFolder("Select worktree location");
        if (!string.IsNullOrEmpty(picked))
        {
            _pathInput.Enter(picked);
            RaiseInputsChanged();
        }
    }

    public string Path => new(_pathInput.Text);
    public string StartPoint => new(_startPointInput.Text);
    public string NewBranchName => new(_branchInput.Text);
    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool CreateEnabled { set => _createButton.IsEnabled.Value = value; }
    public string? ErrorMessage { set => _errorView.Text = value ?? string.Empty; }
    public void FocusPath() => _pathController.BeginEditing();
    public void Close() => _onClose();
}
