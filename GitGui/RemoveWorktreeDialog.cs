using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for `git worktree remove`. Refuses if the worktree has uncommitted
/// changes or untracked files unless Force is checked (delegates the safety check to git).
/// </summary>
public sealed class RemoveWorktreeDialog : MultiChildView, IRemoveWorktreeView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _removeButton;
    private readonly TextView _errorView;

    public event Action? RemoveRequested;

    public RemoveWorktreeDialog(Repo primary, Repo worktree, Action onClose)
    {
        PreferredWidth = 460f;
        _onClose = onClose;

        var title = new TextView
        {
            Text = "Remove worktree",
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
            Text = $"Remove worktree '{worktree.DisplayName}'?",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var pathView = new TextView
        {
            Text = worktree.Path,
            TextColor = DialogPalette.RowTextMissing,
            TextWrap = TextWrap.Wrap,
        };

        var hint = new TextView
        {
            Text = "git refuses if the worktree has uncommitted changes. Check the box to remove anyway.",
            TextColor = DialogPalette.RowTextMissing,
            TextWrap = TextWrap.Wrap,
        };

        _forceCheckbox = new CheckboxView("Remove even if dirty")
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
        _removeButton = new DialogButton("Remove", RaiseRemoveRequested) { PreferredHeight = 32 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = cancelButton },
                new FlexItem { Grow = 1, Child = _removeButton },
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
                        pathView,
                        _forceCheckbox,
                        hint,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new DiscardChangesKbmController(RaiseRemoveRequested, onClose));

        var request = new RemoveWorktreeRequest(primary, worktree);
        this.UsePresenter(ctx => new RemoveWorktreePresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool RemoveEnabled { set => _removeButton.IsEnabled.Value = value; }
    public string? ErrorMessage { set => _errorView.Text = value ?? string.Empty; }

    private void RaiseRemoveRequested() => RemoveRequested?.Invoke();

    public void Close() => _onClose();
}
