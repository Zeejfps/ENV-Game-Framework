using ZGF.Gui;

namespace GitGui;

public sealed class OverlayView : View
{
    private IMessageBus? _messageBus;

    private readonly RectView _background;
    private readonly AddRepoDialog _dialog;
    private bool _isOpen;

    public OverlayView()
    {
        ZIndex = 1000;
        _background = new RectView
        {
            BackgroundColor = 0,
        };
        _dialog = new AddRepoDialog(Close);
        Children.Add(_background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        _messageBus = context.Get<IMessageBus>();
        _messageBus?.Subscribe<AddRepoMessage>(OnAddRepoMessageReceived);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _messageBus?.Unsubscribe<AddRepoMessage>(OnAddRepoMessageReceived);
        _messageBus = null;
    }

    private void OnAddRepoMessageReceived(AddRepoMessage obj)
    {
        Open();
    }

    private void Open()
    {
        if (_isOpen)
            return;
        _isOpen = true;
        _background.BackgroundColor = 0xB0000000;
        Children.Add(_dialog);
    }

    private void Close()
    {
        if (!_isOpen)
            return;
        _isOpen = false;
        _background.BackgroundColor = 0;
        Children.Remove(_dialog);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        _background.LeftConstraint = position.Left;
        _background.BottomConstraint = position.Bottom;
        _background.MinWidthConstraint = position.Width;
        _background.MaxWidthConstraint = position.Width;
        _background.MaxHeightConstraint = position.Height;
        _background.LayoutSelf();

        if (!_isOpen)
            return;

        const float dialogWidth = 360f;
        const float dialogHeight = 230f;
        _dialog.LeftConstraint = position.Left + (position.Width - dialogWidth) * 0.5f;
        _dialog.BottomConstraint = position.Bottom + (position.Height - dialogHeight) * 0.5f;
        _dialog.MinWidthConstraint = dialogWidth;
        _dialog.MaxWidthConstraint = dialogWidth;
        _dialog.MaxHeightConstraint = dialogHeight;
        _dialog.LayoutSelf();
    }
}