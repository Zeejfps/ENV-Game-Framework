using ZGF.Gui;
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
    private const float CloseButtonSize = 28f;
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

        var title = new TextView
        {
            Text = target is null ? "Update all submodules" : "Update submodule",
            TextColor = DialogPalette.TitleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var headerRow = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new MultiChildView { PreferredWidth = CloseButtonSize },
                new FlexItem { Grow = 1, Child = title },
                new DialogCloseButton(onClose),
            },
        };

        var prompt = new TextView
        {
            Text = target is null
                ? $"Run `git submodule update` on every submodule under '{primary.DisplayName}'."
                : $"Run `git submodule update` on '{target.DisplayName}'.",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        _initCheckbox = new CheckboxView("Init missing submodules (--init)") { PreferredHeight = 22 };
        _initCheckbox.IsChecked.Value = true;
        _recursiveCheckbox = new CheckboxView("Recurse into nested submodules (--recursive)") { PreferredHeight = 22 };

        var modeLabel = new TextView
        {
            Text = "Strategy",
            TextColor = DialogPalette.SectionHeaderText,
        };

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

        var conflictsHint = new TextView
        {
            Text = "Merge/rebase strategies may leave the submodule mid-merge on conflict — " +
                   "the Operation banner will offer Abort.",
            TextColor = DialogPalette.RowTextMissing,
            TextWrap = TextWrap.Wrap,
        };

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = 32 };
        _updateButton = new DialogButton("Update", RaiseUpdateRequested) { PreferredHeight = 32 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = cancelButton },
                new FlexItem { Grow = 1, Child = _updateButton },
            },
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(10),
            Padding = PaddingStyle.All(20),
            Children =
            {
                new FlexColumnView
                {
                    Gap = 10,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        headerRow,
                        new RectView { BackgroundColor = DialogPalette.Separator, PreferredHeight = 1 },
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
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new DiscardChangesKbmController(RaiseUpdateRequested, onClose));

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
