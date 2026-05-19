using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal shown after a stash is successfully applied. Lets the user
/// drop the stash (the natural finish of "pop") or keep it around for re-use.
/// Running `git stash drop` here is destructive — the stash cannot be recovered
/// from the UI afterwards.
/// </summary>
public sealed class DropStashDialog : MultiChildView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly DialogButton _dropButton;
    private readonly TextView _errorView;

    private bool _isRunning;

    public DropStashDialog(Repo repo, int index, string label, string subject, Action onClose)
    {
        PreferredWidth = 460f;

        _onClose = onClose;

        var title = new TextView
        {
            Text = $"Drop {label}?",
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
            Text = $"Applied: {subject}\n\nDrop this stash now? This cannot be undone.",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var keepButton = new DialogButton("Keep", onClose)
        {
            PreferredHeight = 32,
        };
        _dropButton = new DialogButton("Drop", () => TryDrop(repo, index))
        {
            PreferredHeight = 32,
        };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = keepButton },
                new FlexItem { Grow = 1, Child = _dropButton },
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
                        _errorView,
                        buttonsRow,
                    },
                },
            },
        });

        // The drop call is small enough to inline here; no presenter needed. We grab
        // services from the context the dialog is attached to.
        this.UsePresenter(ctx => new DropStashPresenter(
            this,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private void TryDrop(Repo repo, int index)
    {
        if (_isRunning) return;
        _isRunning = true;
        _dropButton.IsEnabled.Value = false;
        _errorView.Text = string.Empty;
        DropRequested?.Invoke(repo, index);
    }

    internal event Action<Repo, int>? DropRequested;

    internal void OnDropOutcome(StashOutcome outcome, Repo repo)
    {
        if (!outcome.Success)
        {
            _isRunning = false;
            _dropButton.IsEnabled.Value = true;
            _errorView.Text = outcome.ErrorMessage ?? "Stash drop failed.";
            return;
        }
        _onClose();
    }
}

internal sealed class DropStashPresenter : IDisposable
{
    private readonly DropStashDialog _view;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    public DropStashPresenter(
        DropStashDialog view,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;
        _view.DropRequested += OnDropRequested;
    }

    public void Dispose()
    {
        _view.DropRequested -= OnDropRequested;
    }

    private void OnDropRequested(Repo repo, int index)
    {
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            StashOutcome outcome;
            try { outcome = service.DropStash(repo, index); }
            catch (Exception ex) { outcome = new StashOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                view.OnDropOutcome(outcome, repo);
                if (outcome.Success)
                    bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }
}
