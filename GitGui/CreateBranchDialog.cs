using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user clicks Branch in the actions toolbar. Mirrors Fork's
/// "Create Branch" dialog: branch name + starting point (prefilled with the current HEAD's
/// branch name) + a "checkout after create" checkbox. Runs `git branch &lt;name&gt; &lt;start&gt;` or
/// `git checkout -b &lt;name&gt; &lt;start&gt;` depending on the checkbox.
/// </summary>
public sealed class CreateBranchDialog : MultiChildView, ICreateBranchView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckoutDialogKbmController _nameController;
    private readonly TextInputView _startPointInput;
    private readonly CheckboxView _checkoutCheckbox;
    private readonly DialogButton _createButton;
    private readonly TextView _errorView;

    public event Action? CreateRequested;

    public CreateBranchDialog(Repo repo, string suggestedStartPoint, Action onClose)
    {
        _onClose = onClose;

        var title = new TextView
        {
            Text = "Create branch",
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

        var nameLabel = new TextView
        {
            Text = "Branch name",
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

        var startPointLabel = new TextView
        {
            Text = "Starting point",
            TextColor = DialogPalette.SectionHeaderText,
        };

        _startPointInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
        };
        if (suggestedStartPoint.Length > 0)
            _startPointInput.Enter(suggestedStartPoint);

        var startPointBox = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            PreferredHeight = 28,
            Children = { _startPointInput },
        };

        var startPointHint = new TextView
        {
            Text = "Branch, tag, or commit SHA. Leave blank for HEAD.",
            TextColor = DialogPalette.RowTextMissing,
        };

        _checkoutCheckbox = new CheckboxView("Check out after create")
        {
            PreferredHeight = 22,
            IsChecked =
            {
                Value = true
            }
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
        _createButton = new DialogButton("Create", RaiseCreateRequested)
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
                new FlexItem { Grow = 1, Child = _createButton },
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
                        nameLabel,
                        nameBox,
                        startPointLabel,
                        startPointBox,
                        startPointHint,
                        _checkoutCheckbox,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        // Controllers go on the inputs (not the dialog) — see CheckoutBranchDialog for why:
        // BaseTextInputKbmController consumes left-press anywhere inside the view it's on,
        // so attaching to the outer dialog would swallow clicks meant for Cancel/Create.
        _nameController = new CheckoutDialogKbmController(_nameInput, RaiseCreateRequested, onClose);
        _nameInput.UseController(_ => _nameController);
        _startPointInput.UseController(_ => new CheckoutDialogKbmController(_startPointInput, RaiseCreateRequested, onClose));

        var request = new CreateBranchRequest(repo, suggestedStartPoint);
        this.UsePresenter(ctx => new CreateBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseCreateRequested() => CreateRequested?.Invoke();

    public string Name => new(_nameInput.Text);
    public string StartPoint => new(_startPointInput.Text);
    public bool Checkout => _checkoutCheckbox.IsChecked.Value;
    public bool CreateEnabled
    {
        set => _createButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }
    public event Action NameChanged
    {
        add => _nameInput.TextChanged += value;
        remove => _nameInput.TextChanged -= value;
    }
    public void FocusName()
    {
        _nameController.BeginEditing();
    }
    public void Close() => _onClose();
}
