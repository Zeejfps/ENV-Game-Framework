using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user double-clicks a remote branch that has no matching local
/// branch. Lets them pick the local branch name and whether to set up tracking, then
/// runs `git checkout -b <local> [--track|--no-track] <remote>/<branch>`.
/// </summary>
public sealed class CheckoutBranchDialog : MultiChildView, ICheckoutBranchView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckboxView _trackCheckbox;
    private readonly DialogButton _checkoutButton;

    public event Action? CheckoutRequested;

    public CheckoutBranchDialog(
        Repo repo,
        string remoteName,
        string remoteBranchName,
        string suggestedLocalName,
        Action onClose)
    {
        PreferredWidth = 420f;
        PreferredHeight = 280f;

        _onClose = onClose;

        var title = new TextView
        {
            Text = "Checkout branch",
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

        var subtitle = new TextView
        {
            Text = $"Create a local branch from {remoteName}/{remoteBranchName}",
            TextColor = DialogPalette.BodyText,
        };

        var nameLabel = new TextView
        {
            Text = "Local branch name",
            TextColor = DialogPalette.SectionHeaderText,
        };

        _nameInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
        };

        var nameBox = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            PreferredHeight = 28,
            Children = { _nameInput },
        };

        _trackCheckbox = new CheckboxView("Track this remote branch")
        {
            PreferredHeight = 22,
            IsChecked =
            {
                Value = true
            }
        };

        var cancelButton = new DialogButton("Cancel", onClose)
        {
            PreferredHeight = 32,
        };
        _checkoutButton = new DialogButton("Checkout", RaiseCheckoutRequested)
        {
            PreferredHeight = 32,
        };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = cancelButton },
                new FlexItem { Grow = 1, Child = _checkoutButton },
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
                        subtitle,
                        nameLabel,
                        nameBox,
                        _trackCheckbox,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        // Controller goes on the INPUT's Behaviors, not the outer dialog's:
        // BaseTextInputKbmController.OnMouseButtonStateChanged consumes left-press events
        // anywhere inside the view it's attached to, so putting it on the dialog would
        // swallow clicks meant for the Cancel/Checkout buttons.
        _nameInput.UseController(_ => new CheckoutDialogKbmController(_nameInput, RaiseCheckoutRequested, onClose));

        var request = new CheckoutRequest(repo, remoteName, remoteBranchName, suggestedLocalName);
        this.UsePresenter(ctx => new CheckoutBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseCheckoutRequested() => CheckoutRequested?.Invoke();

    public string Name => new(_nameInput.Text);
    public bool Track => _trackCheckbox.IsChecked.Value;
    public bool CheckoutEnabled
    {
        set => _checkoutButton.IsEnabled.Value = value;
    }
    public event Action NameChanged
    {
        add => _nameInput.TextChanged += value;
        remove => _nameInput.TextChanged -= value;
    }
    public void FocusName(string initialText)
    {
        // Must run after the input is attached to a context — doing it earlier produced an
        // empty-looking field (buffer wrote OK, but StartEditing/StealFocus hadn't run yet
        // so caret/selection visuals were stale and typing didn't engage).
        if (initialText.Length > 0)
            _nameInput.Enter(initialText);
        _nameInput.SelectAll();
        _nameInput.StartEditing();
    }
    public void Close() => _onClose();
}