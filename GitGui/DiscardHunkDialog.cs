using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal sealed class DiscardHunkDialog : MultiChildView, IBind<DiscardHunkViewModel>
{
    private readonly DialogButton _discardButton;
    private readonly TextView _errorView;
    private readonly Action _onClose;

    public DiscardHunkDialog(Repo repo, string path, string patch, Action onClose)
    {
        PreferredWidth = 460f;
        PreferredHeight = 200f;

        _onClose = onClose;

        var prompt = new TextView
        {
            Text = $"Discard this hunk in {path}? This cannot be undone.",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _discardButton = new DialogButton("Discard") { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Discard hunk", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = prompt },
                _errorView,
                DialogFrame.ButtonsRow(cancelButton, _discardButton),
            },
        }));

        this.UseController(_ => new DialogKbmController(_discardButton.Command, _onClose));

        var request = new DiscardHunkRequest(repo, patch);
        this.UseViewModel(
            ctx => new DiscardHunkViewModel(
                request,
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>()),
            Bind);
    }

    public void Bind(DiscardHunkViewModel vm)
    {
        _discardButton.BindCommand(vm.Discard);
        _errorView.BindText(vm.Discard.Error, s => s ?? string.Empty);
        vm.CloseRequested += _onClose;
    }
}

public readonly record struct DiscardHunkRequest(Repo Repo, string Patch);

internal sealed class DiscardHunkViewModel : IDisposable
{
    public AsyncCommand Discard { get; }
    public event Action? CloseRequested;

    public DiscardHunkViewModel(
        DiscardHunkRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        Discard = new AsyncCommand(
            dispatcher,
            work: () => gitService.ApplyPatch(request.Repo, request.Patch, cached: false, reverse: true),
            onSuccess: () =>
            {
                bus.Broadcast(new WorkingTreeChangedMessage(request.Repo.Id));
                CloseRequested?.Invoke();
            });
    }

    public void Dispose() { }
}
