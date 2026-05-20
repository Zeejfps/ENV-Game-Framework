using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Modal shown when the user picks "Rename…" on a local branch row. Full branch path is
/// editable (slashes allowed) so cross-folder moves like feature/login → bugs/login work
/// the same as in `git branch -m`. The force checkbox switches the underlying call to -M,
/// allowing the rename to overwrite an existing branch of the new name.
/// </summary>
public sealed class RenameBranchDialog : MultiChildView, IRenameBranchView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly TextInputView _nameInput;
    private readonly CheckoutDialogKbmController _nameController;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _renameButton;
    private readonly TextView _errorView;
    private readonly string _currentName;

    public event Action? RenameRequested;

    public RenameBranchDialog(Repo repo, string currentName, Action onClose)
    {
        _onClose = onClose;
        _currentName = currentName;

        var title = new TextView
        {
            Text = "Rename branch",
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
            Text = $"Renaming '{currentName}'",
            TextColor = DialogPalette.BodyText,
        };

        var nameLabel = new TextView
        {
            Text = "New name",
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

        _forceCheckbox = new CheckboxView("Force rename even if target exists")
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
        _renameButton = new DialogButton("Rename", RaiseRenameRequested)
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
                new FlexItem { Grow = 1, Child = _renameButton },
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
                        subtitle,
                        nameLabel,
                        nameBox,
                        _forceCheckbox,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        _nameController = new CheckoutDialogKbmController(_nameInput, RaiseRenameRequested, onClose);
        _nameInput.UseController(_ => _nameController);

        var request = new RenameBranchRequest(repo, currentName);
        this.UsePresenter(ctx => new RenameBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void RaiseRenameRequested() => RenameRequested?.Invoke();

    public string Name => new(_nameInput.Text);
    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool RenameEnabled
    {
        set => _renameButton.IsEnabled.Value = value;
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
        _nameInput.Enter(_currentName);
        _nameInput.SelectAll();
        _nameController.BeginEditing();
    }
    public void Close() => _onClose();
}
