using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ForcePushDialog : MultiChildView, IForcePushView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly DialogButton _forcePushButton;
    private readonly TextView _errorView;

    public event Action? ForcePushRequested;

    public ForcePushDialog(Repo repo, string branchName, int ahead, int behind, Action onClose)
    {
        PreferredWidth = 520f;
        PreferredHeight = 260f;

        _onClose = onClose;

        var title = new TextView
        {
            Text = "Force push?",
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

        var displayBranch = string.IsNullOrEmpty(branchName) ? "this branch" : $"'{branchName}'";
        var prompt = new TextView
        {
            Text = $"{displayBranch} has diverged from its upstream — {ahead} ahead, {behind} behind. "
                 + "A regular push will be rejected. Force-push (with lease) will overwrite the remote "
                 + "branch with your local history; any commits on the remote that you haven't fetched "
                 + "will be lost. The lease refuses the push if the remote moved since your last fetch.",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = 32 };
        _forcePushButton = new DialogButton("Force push", RaiseForcePushRequested) { PreferredHeight = 32 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = cancelButton },
                new FlexItem { Grow = 1, Child = _forcePushButton },
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
                        new RectView
                        {
                            BackgroundColor = DialogPalette.Separator,
                            PreferredHeight = 1,
                        },
                        new FlexItem { Grow = 1, Child = prompt },
                        _errorView,
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new AbortOperationKbmController(RaiseForcePushRequested, onClose));

        this.UsePresenter(ctx => new ForcePushPresenter(
            this, repo,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool ForcePushEnabled
    {
        set => _forcePushButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public void Close() => _onClose();

    private void RaiseForcePushRequested() => ForcePushRequested?.Invoke();
}
