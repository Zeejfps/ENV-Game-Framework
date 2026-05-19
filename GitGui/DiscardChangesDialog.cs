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
    private const float CloseButtonSize = 28f;
    private const int MaxListedPaths = 8;

    private readonly Action _onClose;
    private readonly DialogButton _discardButton;
    private readonly TextView _errorView;

    public event Action? DiscardRequested;

    public DiscardChangesDialog(Repo repo, IReadOnlyList<string> paths, Action onClose)
    {
        PreferredWidth = 480f;
        PreferredHeight = 320f;

        _onClose = onClose;

        var title = new TextView
        {
            Text = paths.Count == 1 ? "Discard change" : $"Discard {paths.Count} changes",
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
            Text = "Discarding cannot be undone. Continue?",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var pathList = new TextView
        {
            Text = BuildPathListText(paths),
            TextColor = DialogPalette.RowText,
            TextWrap = TextWrap.Wrap,
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
        _discardButton = new DialogButton("Discard", RaiseDiscardRequested)
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
                new FlexItem { Grow = 1, Child = _discardButton },
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
                        new FlexItem { Grow = 1, Child = pathList },
                        _errorView,
                        buttonsRow,
                    },
                },
            },
        });

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

    private static string BuildPathListText(IReadOnlyList<string> paths)
    {
        if (paths.Count <= MaxListedPaths)
            return string.Join("\n", paths);
        var listed = paths.Take(MaxListedPaths);
        var extra = paths.Count - MaxListedPaths;
        return string.Join("\n", listed) + $"\n…and {extra} more";
    }
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
