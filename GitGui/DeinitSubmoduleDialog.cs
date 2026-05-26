using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for `git submodule deinit` + `git rm`. Refuses if the submodule
/// has uncommitted changes unless Force is checked (delegates the safety check to git).
/// </summary>
public sealed class DeinitSubmoduleDialog : MultiChildView, IDeinitSubmoduleView
{
    private const float DialogWidth = 460f;

    private readonly Action _onClose;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _deinitButton;
    private readonly TextView _errorView;

    public event Action? DeinitRequested;

    public DeinitSubmoduleDialog(Repo primary, Repo submodule, Action onClose)
    {
        PreferredWidth = DialogWidth;
        _onClose = onClose;

        var prompt = new TextView
        {
            Text = $"Deinit and remove submodule '{submodule.DisplayName}'?",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var detail = new TextView
        {
            Text = "Runs `git submodule deinit` followed by `git rm`. The submodule will " +
                   "be removed from the working tree and the deletion staged in the parent " +
                   "for your next commit.",
            TextWrap = TextWrap.Wrap,
        };
        detail.BindTextColorFromTheme(t => t.Dialog.RowTextMissing);

        _forceCheckbox = new CheckboxView("Deinit even if dirty")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _deinitButton = new DialogButton("Deinit", RaiseDeinitRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Deinit submodule", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                detail,
                _forceCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _deinitButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(RaiseDeinitRequested, onClose));

        var request = new DeinitSubmoduleViewRequest(primary, submodule);
        this.UsePresenter(ctx => new DeinitSubmodulePresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool DeinitEnabled { set => _deinitButton.IsEnabled.Value = value; }
    public string? ErrorMessage { set => _errorView.Text = value ?? string.Empty; }

    private void RaiseDeinitRequested() => DeinitRequested?.Invoke();

    public void Close() => _onClose();
}
