using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for discarding unstaged changes. Lists the affected paths so the
/// user can verify what's about to be thrown away, with a Cancel/Discard pair. Discard
/// is a destructive action — the worktree changes (and any untracked files in the set)
/// cannot be recovered from git afterwards.
/// </summary>
internal sealed class DiscardChangesDialog : MultiChildView, IBind<DiscardChangesViewModel>
{
    private readonly DialogButton _discardButton;
    private readonly TextView _errorView;
    private readonly Action _onClose;

    public DiscardChangesDialog(Repo repo, IReadOnlyList<string> paths, Action onClose)
    {
        PreferredWidth = 480f;
        PreferredHeight = 320f;

        _onClose = onClose;

        var title = paths.Count == 1 ? "Discard change" : $"Discard {paths.Count} changes";

        var prompt = new TextView
        {
            Text = "Discarding cannot be undone. Continue?",
            TextWrap = TextWrap.Wrap,
        };
        prompt.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var pathList = new TextView
        {
            Text = string.Join("\n", paths),
            TextWrap = TextWrap.Wrap,
        };
        pathList.BindTextColorFromTheme(t => t.Dialog.RowText);

        var scrollPane = new VerticalScrollPane();
        scrollPane.Children.Add(new PaddingView
        {
            Padding = new PaddingStyle { Left = 8, Right = 8, Top = 6, Bottom = 6 },
            Children = { pathList },
        });
        scrollPane.UseController(_ => new VerticalScrollPaneWheelController(scrollPane));

        var vScrollBar = ScrollBarStyles.CreateVertical();

        var scrollHost = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(4),
            Children =
            {
                new BorderLayoutView
                {
                    Center = scrollPane,
                    East = vScrollBar,
                },
            },
        };
        scrollHost.BindBackgroundColorFromTheme(t => t.Surfaces.BgDeep);
        scrollHost.BindBorderColorFromTheme(t => BorderColorStyle.All(t.Dialog.Border));
        scrollHost.UsePresenter(_ => new VerticalScrollBarSyncController(scrollPane, vScrollBar));

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _discardButton = new DialogButton("Discard") { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build(title, onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                prompt,
                new FlexItem { Grow = 1, Child = scrollHost },
                _errorView,
                DialogFrame.ButtonsRow(cancelButton, _discardButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(_discardButton.Command, _onClose));

        var request = new DiscardChangesRequest(repo, paths);
        this.UseViewModel(
            ctx => new DiscardChangesViewModel(
                request,
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>()),
            Bind);
    }

    public void Bind(DiscardChangesViewModel vm)
    {
        _discardButton.BindCommand(vm.Discard);
        _errorView.BindText(vm.Discard.Error, s => s ?? string.Empty);
        vm.CloseRequested += _onClose;
    }
}

public readonly record struct DiscardChangesRequest(Repo Repo, IReadOnlyList<string> Paths);
