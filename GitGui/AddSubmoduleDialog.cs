using ZGF.Gui;
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

        _urlInput = MakeTextInput();
        _pathInput = MakeTextInput();
        _branchInput = MakeTextInput();

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
                Label("Repository URL"),
                WrapInput(_urlInput),
                Label("Path inside parent"),
                WrapInput(_pathInput),
                Hint("Where to clone the submodule, relative to the parent root."),
                Label("Track branch (optional)"),
                WrapInput(_branchInput),
                Hint("Leave blank to pin to the upstream HEAD at clone time."),
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

    private static TextView Label(string text) => new()
    {
        Text = text,
        TextColor = DialogPalette.SectionHeaderText,
    };

    private static TextView Hint(string text) => new()
    {
        Text = text,
        TextColor = DialogPalette.RowTextMissing,
    };

    private static TextInputView MakeTextInput() => new()
    {
        BackgroundColor = DialogPalette.ButtonNormal,
        TextColor = DialogPalette.TitleText,
        CaretColor = DialogPalette.TitleText,
        SelectionRectColor = DialogPalette.RowActive,
        TextWrap = TextWrap.NoWrap,
    };

    private static View WrapInput(TextInputView input) => new RectView
    {
        BackgroundColor = DialogPalette.ButtonNormal,
        BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
        BorderSize = BorderSizeStyle.All(1),
        BorderRadius = BorderRadiusStyle.All(3),
        Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
        PreferredHeight = 28,
        Children = { input },
    };

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
