using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown from "Update all submodules…" on a primary repo or "Update submodule…"
/// on an individual submodule row. Lets the user pick init / recursive flags plus an
/// update strategy (checkout / merge / rebase).
/// </summary>
internal sealed class UpdateSubmodulesDialog : MultiChildView, IBind<UpdateSubmodulesDialogViewModel>
{
    private const float DialogWidth = 480f;

    private readonly Action _onClose;
    private readonly CheckboxView _initCheckbox;
    private readonly CheckboxView _recursiveCheckbox;
    private readonly CheckboxView _checkoutMode;
    private readonly CheckboxView _mergeMode;
    private readonly CheckboxView _rebaseMode;
    private readonly DialogButton _updateButton;
    private readonly TextView _errorView;
    private UpdateSubmodulesDialogViewModel? _vm;

    public UpdateSubmodulesDialog(Repo primary, Repo? target, Action onClose)
    {
        PreferredWidth = DialogWidth;
        _onClose = onClose;

        var titleText = target is null ? "Update all submodules" : "Update submodule";

        var prompt = new TextView
        {
            Text = target is null
                ? $"Run `git submodule update` on every submodule under '{primary.DisplayName}'."
                : $"Run `git submodule update` on '{target.DisplayName}'.",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindThemedTextColor(s => s.DialogBody.BodyText);

        _initCheckbox = new CheckboxView("Init missing submodules (--init)") { PreferredHeight = 22 };
        _recursiveCheckbox = new CheckboxView("Recurse into nested submodules (--recursive)") { PreferredHeight = 22 };

        var modeLabel = DialogFrame.Label("Strategy");

        _checkoutMode = new CheckboxView("Checkout (default — reset to recorded SHA)") { PreferredHeight = 22 };
        _mergeMode = new CheckboxView("Merge (--merge)") { PreferredHeight = 22 };
        _rebaseMode = new CheckboxView("Rebase (--rebase)") { PreferredHeight = 22 };

        var conflictsHint = DialogFrame.Hint(
            "Merge/rebase strategies may leave the submodule mid-merge on conflict — " +
            "the Operation banner will offer Abort.",
            TextWrap.Wrap);

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _updateButton = new DialogButton("Update") { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build(titleText, onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                _initCheckbox,
                _recursiveCheckbox,
                modeLabel,
                _checkoutMode,
                _mergeMode,
                _rebaseMode,
                conflictsHint,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _updateButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(Submit, onClose));

        var request = new UpdateSubmodulesViewRequest(primary, target);
        this.UseViewModel(
            ctx => new UpdateSubmodulesDialogViewModel(
                request,
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>()),
            Bind);
    }

    public void Bind(UpdateSubmodulesDialogViewModel vm)
    {
        _vm = vm;
        vm.CloseRequested += _onClose;

        _initCheckbox.IsChecked.BindTwoWay(vm.Init);
        _recursiveCheckbox.IsChecked.BindTwoWay(vm.Recursive);
        _updateButton.BindCommand(vm.Update);
        _errorView.BindText(vm.Error, s => s ?? string.Empty);

        vm.Mode.Subscribe(m =>
        {
            _checkoutMode.IsChecked.Value = m == SubmoduleUpdateMode.Checkout;
            _mergeMode.IsChecked.Value = m == SubmoduleUpdateMode.Merge;
            _rebaseMode.IsChecked.Value = m == SubmoduleUpdateMode.Rebase;
        });
        _checkoutMode.IsChecked.Changed += b => SelectMode(vm, SubmoduleUpdateMode.Checkout, b);
        _mergeMode.IsChecked.Changed += b => SelectMode(vm, SubmoduleUpdateMode.Merge, b);
        _rebaseMode.IsChecked.Changed += b => SelectMode(vm, SubmoduleUpdateMode.Rebase, b);
    }

    private void Submit() => _vm?.Update.Execute();

    private void SelectMode(UpdateSubmodulesDialogViewModel vm, SubmoduleUpdateMode mode, bool isCheckedNow)
    {
        if (!isCheckedNow)
        {
            if (vm.Mode.Value == mode)
                CheckboxFor(mode).IsChecked.Value = true;
            return;
        }
        if (vm.Mode.Value == mode) return;
        vm.Mode.Value = mode;
    }

    private CheckboxView CheckboxFor(SubmoduleUpdateMode mode) => mode switch
    {
        SubmoduleUpdateMode.Merge => _mergeMode,
        SubmoduleUpdateMode.Rebase => _rebaseMode,
        _ => _checkoutMode,
    };
}
