using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for deleting a local branch. Default uses `git branch -d` which
/// refuses if the branch isn't fully merged into upstream/HEAD; the force checkbox
/// switches to `-D` which deletes anyway (the destructive option, off by default).
/// </summary>
public sealed class DeleteLocalBranchDialog : MultiChildView, IDeleteLocalBranchView
{
    private readonly Action _onClose;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _deleteButton;
    private readonly TextView _errorView;

    public event Action? DeleteRequested;

    public DeleteLocalBranchDialog(Repo repo, string branchName, Action onClose)
    {
        PreferredWidth = 460f;

        _onClose = onClose;

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

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _deleteButton = new DialogButton("Delete", RaiseDeleteRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Delete branch", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                _forceCheckbox,
                hint,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _deleteButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(RaiseDeleteRequested, onClose));

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
