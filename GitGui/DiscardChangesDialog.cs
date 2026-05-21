using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for discarding unstaged changes. Lists the affected paths so the
/// user can verify what's about to be thrown away, with a Cancel/Discard pair. Discard
/// is a destructive action — the worktree changes (and any untracked files in the set)
/// cannot be recovered from git afterwards.
/// </summary>
public sealed class DiscardChangesDialog : MultiChildView, IDiscardChangesView
{
    private readonly Action _onClose;
    private readonly DialogButton _discardButton;
    private readonly TextView _errorView;

    public event Action? DiscardRequested;

    public DiscardChangesDialog(Repo repo, IReadOnlyList<string> paths, Action onClose)
    {
        PreferredWidth = 480f;
        PreferredHeight = 320f;

        _onClose = onClose;

        var title = paths.Count == 1 ? "Discard change" : $"Discard {paths.Count} changes";

        var prompt = new TextView
        {
            Text = "Discarding cannot be undone. Continue?",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var pathList = new TextView
        {
            Text = string.Join("\n", paths),
            TextColor = DialogPalette.RowText,
            TextWrap = TextWrap.Wrap,
        };

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
            BackgroundColor = Theme.BgDeep,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
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
        scrollHost.UsePresenter(_ => new VerticalScrollBarSyncController(scrollPane, vScrollBar));

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _discardButton = new DialogButton("Discard", RaiseDiscardRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

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

        this.UseController(_ => new DiscardChangesKbmController(RaiseDiscardRequested, onClose));

        var request = new DiscardChangesRequest(repo, paths);
        this.UsePresenter(ctx => new DiscardChangesPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool DiscardEnabled
    {
        set => _discardButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public void Close() => _onClose();

    private void RaiseDiscardRequested() => DiscardRequested?.Invoke();
}

internal sealed class DiscardChangesKbmController : KeyboardMouseController
{
    private readonly Action _onConfirm;
    private readonly Action _onCancel;

    public DiscardChangesKbmController(Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (e.Key == KeyboardKey.Escape)
        {
            e.Consume();
            _onCancel();
        }
        else if (e.Key == KeyboardKey.Enter || e.Key == KeyboardKey.NumpadEnter)
        {
            e.Consume();
            _onConfirm();
        }
    }
}

public readonly record struct DiscardChangesRequest(Repo Repo, IReadOnlyList<string> Paths);
