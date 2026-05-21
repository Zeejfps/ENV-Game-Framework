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
    private const float CloseButtonSize = 28f;
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

        var title = new TextView
        {
            Text = "Deinit submodule",
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
            Text = $"Deinit and remove submodule '{submodule.DisplayName}'?",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var detail = new TextView
        {
            Text = "Runs `git submodule deinit` followed by `git rm`. The submodule will " +
                   "be removed from the working tree and the deletion staged in the parent " +
                   "for your next commit.",
            TextColor = DialogPalette.RowTextMissing,
            TextWrap = TextWrap.Wrap,
        };

        _forceCheckbox = new CheckboxView("Deinit even if dirty")
        {
            PreferredHeight = 22,
        };

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = 32 };
        _deinitButton = new DialogButton("Deinit", RaiseDeinitRequested) { PreferredHeight = 32 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = cancelButton },
                new FlexItem { Grow = 1, Child = _deinitButton },
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
                    Gap = 12,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        headerRow,
                        new RectView { BackgroundColor = DialogPalette.Separator, PreferredHeight = 1 },
                        prompt,
                        detail,
                        _forceCheckbox,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new DiscardChangesKbmController(RaiseDeinitRequested, onClose));

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
