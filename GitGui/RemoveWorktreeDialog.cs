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
    private const float DialogWidth = 460f;
    private const float CodeBlockInnerPadding = 8f;

    private readonly string _path;
    private readonly Action _onClose;
    private readonly CheckboxView _forceCheckbox;
    private readonly DialogButton _removeButton;
    private readonly TextView _errorView;
    private readonly TextView _pathTextView;
    private readonly TextStyle _pathTextStyle;

    public event Action? RemoveRequested;

    public RemoveWorktreeDialog(Repo primary, Repo worktree, Action onClose)
    {
        PreferredWidth = DialogWidth;
        _onClose = onClose;
        _path = worktree.Path;

        var prompt = new TextView
        {
            Text = $"Remove worktree '{worktree.DisplayName}'?",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindTextColorFromTheme(t => t.Dialog.BodyText);

        _pathTextStyle = new TextStyle
        {
            TextColor = ThemePresets.Dark.Dialog.BodyText,
            FontFamily = DiffOptions.MonoFontFamily,
            FontSize = 12f,
            TextWrap = TextWrap.Wrap,
        };
        _pathTextView = new TextView
        {
            Text = worktree.Path,
            FontFamily = DiffOptions.MonoFontFamily,
            FontSize = 12f,
            TextWrap = TextWrap.Wrap,
        };
        _pathTextView.BindToTheme(t =>
        {
            _pathTextView.TextColor = t.Dialog.BodyText;
            _pathTextStyle.TextColor = t.Dialog.BodyText;
        });
        var pathBox = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle
            {
                Left = (int)CodeBlockInnerPadding,
                Right = (int)CodeBlockInnerPadding,
                Top = 6,
                Bottom = 6,
            },
            Children = { _pathTextView },
        };
        pathBox.BindBackgroundColorFromTheme(t => t.Surfaces.BgDeep);
        pathBox.BindBorderColorFromTheme(t => BorderColorStyle.All(t.Dialog.Border));

        var hint = new TextView
        {
            Text = "git refuses if the worktree has uncommitted changes. Check the box to remove anyway.",
            TextWrap = TextWrap.Wrap,
        };
        hint.BindTextColorFromTheme(t => t.Dialog.RowTextMissing);

        _forceCheckbox = new CheckboxView("Remove even if dirty")
        {
            PreferredHeight = 22,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _removeButton = new DialogButton("Remove", RaiseRemoveRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        var dialogBody = DialogFrame.Build("Remove worktree", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                pathBox,
                _forceCheckbox,
                hint,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _removeButton),
            },
        });

        // ClippingView wraps the dialog so a child that measures too wide (e.g. a path that
        // can't be word-broken because it has no spaces) still can't draw past the dialog's
        // rounded edge. The path block also does its own pre-wrap on attach below.
        var clip = new ClippingView();
        clip.Children.Add(dialogBody);
        AddChildToSelf(clip);

        this.UseController(_ => new DialogKbmController(RaiseRemoveRequested, onClose));

        var request = new RemoveWorktreeRequest(primary, worktree);
        this.UsePresenter(ctx => new RemoveWorktreePresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        // Path strings have no whitespace, so the framework's word-wrap engine can't break
        // them. Pre-wrap by inserting newlines at path-separator boundaries so the displayed
        // block stays inside the dialog's content width.
        var available = DialogWidth
                        - 2 * DialogFrame.DefaultPadding
                        - 2 * CodeBlockInnerPadding
                        - 2; // account for the 1px border on each side of the code-block
        _pathTextView.Text = PathWrap.Wrap(_path, _pathTextStyle, available, context.Canvas);
    }

    public bool Force => _forceCheckbox.IsChecked.Value;
    public bool RemoveEnabled { set => _removeButton.IsEnabled.Value = value; }
    public string? ErrorMessage { set => _errorView.Text = value ?? string.Empty; }

    private void RaiseRemoveRequested() => RemoveRequested?.Invoke();

    public void Close() => _onClose();
}
