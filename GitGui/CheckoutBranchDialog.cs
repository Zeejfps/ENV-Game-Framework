using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user double-clicks a remote branch that has no matching local
/// branch. Lets them pick the local branch name and whether to set up tracking, then
/// runs `git checkout -b <local> [--track|--no-track] <remote>/<branch>`.
/// </summary>
public sealed class CheckoutBranchDialog : MultiChildView
{
    private const float CloseButtonSize = 28f;

    private readonly Repo _repo;
    private readonly string _remoteName;
    private readonly string _remoteBranchName;
    private readonly string _suggestedLocalName;
    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckboxView _trackCheckbox;
    private readonly DialogButton _checkoutButton;
    private readonly CheckoutDialogKbmController _inputController;

    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IMessageBus? _bus;
    private bool _isCheckingOut;

    public CheckoutBranchDialog(
        Repo repo,
        string remoteName,
        string remoteBranchName,
        string suggestedLocalName,
        Action onClose)
    {
        _repo = repo;
        _remoteName = remoteName;
        _remoteBranchName = remoteBranchName;
        _suggestedLocalName = suggestedLocalName;
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
        // Pre-fill + select-all is deferred to OnAttachedToContext: doing it in the
        // constructor before the input is attached to a context produced an empty-looking
        // field (the buffer wrote OK, but StartEditing/StealFocus hadn't run yet so the
        // caret/selection visuals were stale and typing didn't engage).

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
        };
        _trackCheckbox.IsChecked.Value = true;

        var cancelButton = new DialogButton("Cancel", onClose)
        {
            PreferredHeight = 32,
        };
        _checkoutButton = new DialogButton("Checkout", TryCheckout)
        {
            PreferredHeight = 32,
        };
        _checkoutButton.IsEnabled.Value = suggestedLocalName.Length > 0;
        _nameInput.TextChanged += UpdateCheckoutEnabled;

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
        _inputController = new CheckoutDialogKbmController(_nameInput, TryCheckout, onClose);
        _nameInput.Behaviors.Add(_inputController);
    }

    protected override void OnAttachedToContext(Context context)
    {
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _bus = context.Get<IMessageBus>();

        // Auto-focus and pre-fill happen here (not in the constructor) so the input is
        // fully attached when we wire up its editing state.
        if (_suggestedLocalName.Length > 0)
            _nameInput.Enter(_suggestedLocalName.AsSpan());
        _nameInput.SelectAll();
        _nameInput.StartEditing();
        context.StealFocus(_inputController);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _gitService = null;
        _dispatcher = null;
        _bus = null;
    }

    private void UpdateCheckoutEnabled()
    {
        if (_isCheckingOut) return;
        _checkoutButton.IsEnabled.Value = _nameInput.Text.Length > 0;
    }

    private void TryCheckout()
    {
        if (_isCheckingOut) return;
        var localName = new string(_nameInput.Text);
        if (localName.Length == 0) return;

        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        if (service == null || dispatcher == null || bus == null) return;

        _isCheckingOut = true;
        _checkoutButton.IsEnabled.Value = false;

        var repo = _repo;
        var remoteName = _remoteName;
        var remoteBranchName = _remoteBranchName;
        var track = _trackCheckbox.IsChecked.Value;

        Task.Run(() =>
        {
            CheckoutOutcome outcome;
            try
            {
                outcome = service.CheckoutRemoteBranch(repo, localName, remoteName, remoteBranchName, track);
            }
            catch (Exception ex)
            {
                outcome = new CheckoutOutcome(false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                _isCheckingOut = false;
                // Close before broadcasting: the error broadcast triggers OverlayView to
                // swap in CheckoutErrorDialog, and a stale _onClose() afterwards would
                // remove the brand-new error dialog instead of this one.
                _onClose();
                if (outcome.Success)
                    bus.Broadcast(new RefsChangedMessage(repo.Id));
                else
                    bus.Broadcast(new ShowCheckoutErrorMessage(
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
        });
    }
}

internal sealed class CheckoutDialogKbmController : BaseTextInputKbmController
{
    private readonly Action _onSubmit;
    private readonly Action _onCancel;

    public CheckoutDialogKbmController(TextInputView input, Action onSubmit, Action onCancel) : base(input)
    {
        _onSubmit = onSubmit;
        _onCancel = onCancel;
    }

    protected override void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        if (e.Key == KeyboardKey.Enter || e.Key == KeyboardKey.NumpadEnter)
        {
            e.Consume();
            _onSubmit();
            return;
        }
        if (e.Key == KeyboardKey.Escape)
        {
            e.Consume();
            _onCancel();
            return;
        }
        base.OnKeyboardKeyPressed(ref e);
    }
}
