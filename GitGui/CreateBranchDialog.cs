using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user clicks Branch in the actions toolbar. Mirrors Fork's
/// "Create Branch" dialog: branch name + starting point (prefilled with the current HEAD's
/// branch name) + a "checkout after create" checkbox. Runs `git branch &lt;name&gt; &lt;start&gt;` or
/// `git checkout -b &lt;name&gt; &lt;start&gt;` depending on the checkbox.
/// </summary>
public sealed class CreateBranchDialog : MultiChildView, ICreateBranchView
{
    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckoutDialogKbmController _nameController;
    private readonly TextInputView _startPointInput;
    private readonly CheckboxView _checkoutCheckbox;
    private readonly DialogButton _createButton;
    private readonly TextView _errorView;

    public event Action? CreateRequested;

    public CreateBranchDialog(Repo repo, string suggestedStartPoint, Action onClose)
    {
        _onClose = onClose;

        var nameLabel = new TextView { Text = "Branch name" };
        nameLabel.BindThemedTextColor(s => s.DialogBody.SectionHeaderText);

        _nameInput = DialogFrame.TextInput();
        var nameBox = DialogFrame.WrapInput(_nameInput);

        var startPointLabel = new TextView { Text = "Starting point" };
        startPointLabel.BindThemedTextColor(s => s.DialogBody.SectionHeaderText);

        _startPointInput = DialogFrame.TextInput();
        if (suggestedStartPoint.Length > 0)
            _startPointInput.Enter(suggestedStartPoint);
        var startPointBox = DialogFrame.WrapInput(_startPointInput);

        var startPointHint = new TextView { Text = "Branch, tag, or commit SHA. Leave blank for HEAD." };
        startPointHint.BindThemedTextColor(s => s.DialogBody.RowTextMissing);

        _checkoutCheckbox = new CheckboxView("Check out after create")
        {
            PreferredHeight = 22,
            IsChecked =
            {
                Value = true
            }
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _createButton = new DialogButton("Create", RaiseCreateRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Create branch", onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                nameLabel,
                nameBox,
                startPointLabel,
                startPointBox,
                startPointHint,
                _checkoutCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _createButton),
            },
        }));

        // Controllers go on the inputs (not the dialog) — see CheckoutBranchDialog for why:
        // BaseTextInputKbmController consumes left-press anywhere inside the view it's on,
        // so attaching to the outer dialog would swallow clicks meant for Cancel/Create.
        _nameController = new CheckoutDialogKbmController(_nameInput, RaiseCreateRequested, onClose);
        _nameInput.UseController(_ => _nameController);
        _startPointInput.UseController(_ => new CheckoutDialogKbmController(_startPointInput, RaiseCreateRequested, onClose));

        var request = new CreateBranchRequest(repo, suggestedStartPoint);
        this.UsePresenter(ctx => new CreateBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseCreateRequested() => CreateRequested?.Invoke();

    public string Name => new(_nameInput.Text);
    public string StartPoint => new(_startPointInput.Text);
    public bool Checkout => _checkoutCheckbox.IsChecked.Value;
    public bool CreateEnabled
    {
        set => _createButton.IsEnabled.Value = value;
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
        _nameController.BeginEditing();
    }
    public void Close() => _onClose();
}
