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
public sealed class UpdateSubmodulesDialog : MultiChildView, IUpdateSubmodulesView
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

    private SubmoduleUpdateMode _mode = SubmoduleUpdateMode.Checkout;

    public event Action? UpdateRequested;

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
        _initCheckbox.IsChecked.Value = true;
        _recursiveCheckbox = new CheckboxView("Recurse into nested submodules (--recursive)") { PreferredHeight = 22 };

        var modeLabel = DialogFrame.Label("Strategy");

        _checkoutMode = new CheckboxView("Checkout (default — reset to recorded SHA)") { PreferredHeight = 22 };
        _checkoutMode.IsChecked.Value = true;
        _mergeMode = new CheckboxView("Merge (--merge)") { PreferredHeight = 22 };
        _rebaseMode = new CheckboxView("Rebase (--rebase)") { PreferredHeight = 22 };

        // Mutual exclusivity — clicking one selects it and unchecks the others. Re-clicking
        // an already-checked option restores it (we never allow "no mode" — defaults to
        // Checkout). Subscribing on each per-mode checkbox keeps the rule local.
        _checkoutMode.IsChecked.Subscribe(v => SelectMode(SubmoduleUpdateMode.Checkout, v));
        _mergeMode.IsChecked.Subscribe(v => SelectMode(SubmoduleUpdateMode.Merge, v));
        _rebaseMode.IsChecked.Subscribe(v => SelectMode(SubmoduleUpdateMode.Rebase, v));

        var conflictsHint = DialogFrame.Hint(
            "Merge/rebase strategies may leave the submodule mid-merge on conflict — " +
            "the Operation banner will offer Abort.",
            TextWrap.Wrap);

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _updateButton = new DialogButton("Update", RaiseUpdateRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

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

        this.UseController(_ => new DialogKbmController(RaiseUpdateRequested, onClose));

        var request = new UpdateSubmodulesViewRequest(primary, target);
        this.UsePresenter(ctx => new UpdateSubmodulesPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void SelectMode(SubmoduleUpdateMode mode, bool isCheckedNow)
    {
        // A user toggle that turns off the current mode is treated as "stay selected" —
        // we re-check it. Anything else: switch to the new mode and uncheck siblings.
        if (!isCheckedNow)
        {
            if (_mode == mode) ApplyMode(_mode);
            return;
        }
        if (_mode == mode) return;
        _mode = mode;
        ApplyMode(mode);
    }

    private void ApplyMode(SubmoduleUpdateMode mode)
    {
        _checkoutMode.IsChecked.Value = mode == SubmoduleUpdateMode.Checkout;
        _mergeMode.IsChecked.Value = mode == SubmoduleUpdateMode.Merge;
        _rebaseMode.IsChecked.Value = mode == SubmoduleUpdateMode.Rebase;
    }

    public bool Init => _initCheckbox.IsChecked.Value;
    public bool Recursive => _recursiveCheckbox.IsChecked.Value;
    public SubmoduleUpdateMode Mode => _mode;
    public bool UpdateEnabled { set => _updateButton.IsEnabled.Value = value; }
    public string? ErrorMessage { set => _errorView.Text = value ?? string.Empty; }

    private void RaiseUpdateRequested() => UpdateRequested?.Invoke();

    public void Close() => _onClose();
}
