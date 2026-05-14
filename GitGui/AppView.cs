using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class AppView : View
{
    public AppView()
    {
        Children.Add(new BorderLayoutView
        {
            West = new RepoBar(),
        });
        Children.Add(new OverlayView());
    }
}

public sealed class OverlayView : View
{
    private IMessageBus? _messageBus;

    private readonly RectView _background;
    
    public OverlayView()
    {
        _background = new RectView
        {
            BackgroundColor = 0,
        };
        Children.Add(_background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        Console.WriteLine("Overlay view attached to context");
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
        Console.WriteLine("Add repo message received");
        _background.BackgroundColor = 0xFF00FF00;
    }
}

public sealed class RepoBar : View
{
    public RepoBar()
    {
        PreferredWidth = 80;
        AddChildToSelf(new RectView
        {
            BackgroundColor = 0xFF0000FF,
            Padding = PaddingStyle.All(5),
            Children =
            {
                new ColumnView
                {
                    Children =
                    {
                        new AddRepoButton(),
                    }
                }
            }
        });
    }
}

public sealed class AddRepoButton : View
{
    public AddRepoButton()
    {
        PreferredHeight = 70;
        Children.Add(new RectView
        {
            BackgroundColor = 0xFFFF22FF,
        });
        Behaviors.Add(new AddRepoButtonController());
    }
}

public sealed class AddRepoButtonController : KeyboardMouseController
{
    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            Context?.Get<IMessageBus>()?.Broadcast<AddRepoMessage>();
            e.Consume();
        }
    }
}

public record struct AddRepoMessage;