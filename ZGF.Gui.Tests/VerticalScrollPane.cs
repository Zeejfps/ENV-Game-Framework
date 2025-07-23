using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollPane : Component, IKeyboardMouseController
{
    private float _yOffset;
    public float YOffset
    {
        get => _yOffset;
        set => SetField(ref _yOffset, value);
    }

    public VerticalScrollPane()
    {
        Controller = this;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        foreach (var child in Children)
        {
            child.BottomConstraint = position.Bottom + _yOffset;
            child.LeftConstraint = position.Left;            
            child.MinWidthConstraint = position.Width;
            child.MaxWidthConstraint = position.Width;
            child.LayoutSelf();
        }
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        c.PushClip(Position);
        base.OnDrawChildren(c);
        c.PopClip();
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        base.OnDrawSelf(c);
        c.AddCommand(new DrawRectCommand
        {
            Position = Position,
            Style = new RectStyle
            {
                BackgroundColor = 0x00FF00,
            },
            ZIndex = 1
        });
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public Component Component => this;
    public void OnMouseEnter(in MouseEnterEvent e)
    {
        this.RequestFocus();
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        this.Blur();
    }

    public bool OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            if (e.Key == KeyboardKey.UpArrow)
            {
                YOffset += 10f;
            }
            else if (e.Key == KeyboardKey.DownArrow)
            {
                YOffset -= 10f;
            }
        }
        return true;
    }
}

public sealed class ScrollView : Component
{
    private readonly VerticalScrollPane _viewPort;
    
    public Component? Content { get; set; }
    
    public ScrollView()
    {
        _viewPort = new VerticalScrollPane();
        Add(_viewPort);
    }
}