using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user clicks Stash in the actions toolbar. Lets the user name the
/// stash, optionally include untracked files (-u), and optionally keep the index
/// (--keep-index) so staged hunks stay around after stashing. Runs `git stash push`.
/// </summary>
public sealed class StashDialog : MultiChildView, IStashView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly TextInputView _messageInput;
    private readonly CheckboxView _includeUntrackedCheckbox;
    private readonly CheckboxView _keepStagedCheckbox;
    private readonly DialogButton _stashButton;
    private readonly TextView _errorView;

    public event Action? StashRequested;

    public StashDialog(Repo repo, Action onClose)
    {
        _onClose = onClose;

        var title = new TextView
        {
            Text = "Stash changes",
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

        var messageLabel = new TextView
        {
            Text = "Message",
            TextColor = DialogPalette.SectionHeaderText,
        };

        _messageInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
        };

        var messageBox = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            PreferredHeight = 28,
            Children = { _messageInput },
        };

        _includeUntrackedCheckbox = new CheckboxView("Include untracked files")
        {
            PreferredHeight = 22,
        };
        _keepStagedCheckbox = new CheckboxView("Keep staged changes in index")
        {
            PreferredHeight = 22,
        };

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose)
        {
            PreferredHeight = 32,
        };
        _stashButton = new DialogButton("Stash", RaiseStashRequested)
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
                new FlexItem { Grow = 1, Child = _stashButton },
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
                        new RectView
                        {
                            BackgroundColor = DialogPalette.Separator,
                            PreferredHeight = 1,
                        },
                        messageLabel,
                        messageBox,
                        _includeUntrackedCheckbox,
                        _keepStagedCheckbox,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        // Same reason as CreateBranchDialog: text-input controllers consume clicks across
        // the view they're on, so attach to the input itself, not the outer dialog.
        _messageInput.UseController(_ => new CheckoutDialogKbmController(_messageInput, RaiseStashRequested, onClose));

        var request = new StashRequest(repo);
        this.UsePresenter(ctx => new StashPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseStashRequested() => StashRequested?.Invoke();

    public string Message => new(_messageInput.Text);
    public bool IncludeUntracked => _includeUntrackedCheckbox.IsChecked.Value;
    public bool KeepStaged => _keepStagedCheckbox.IsChecked.Value;
    public bool StashEnabled
    {
        set => _stashButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }
    public event Action MessageChanged
    {
        add => _messageInput.TextChanged += value;
        remove => _messageInput.TextChanged -= value;
    }
    public void FocusMessage()
    {
        _messageInput.StartEditing();
    }
    public void Close() => _onClose();
}
