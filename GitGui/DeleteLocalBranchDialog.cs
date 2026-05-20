using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for deleting a local branch. Default uses `git branch -d` which
/// refuses if the branch isn't fully merged into upstream/HEAD; the force checkbox
/// switches to `-D` which deletes anyway (the destructive option, off by default).
/// </summary>
public sealed class DeleteLocalBranchDialog : MultiChildView, IDeleteLocalBranchView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _deleteButton;
    private readonly TextView _errorView;

    public event Action? DeleteRequested;

    public DeleteLocalBranchDialog(Repo repo, string branchName, Action onClose)
    {
        PreferredWidth = 460f;

        _onClose = onClose;

        var title = new TextView
        {
            Text = "Delete branch",
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
            Text = $"Delete local branch '{branchName}'?",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var hint = new TextView
        {
            Text = "Unchecked: refuses if the branch isn't fully merged into its upstream or HEAD.",
            TextColor = DialogPalette.RowTextMissing,
            TextWrap = TextWrap.Wrap,
        };

        _forceCheckbox = new CheckboxView("Delete even if not merged")
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
        _deleteButton = new DialogButton("Delete", RaiseDeleteRequested)
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
                new FlexItem { Grow = 1, Child = _deleteButton },
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
                        prompt,
                        _forceCheckbox,
                        hint,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new DiscardChangesKbmController(RaiseDeleteRequested, onClose));

        var request = new DeleteLocalBranchRequest(repo, branchName);
        this.UsePresenter(ctx => new DeleteLocalBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool DeleteEnabled
    {
        set => _deleteButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    private void RaiseDeleteRequested() => DeleteRequested?.Invoke();

    public void Close() => _onClose();
}
