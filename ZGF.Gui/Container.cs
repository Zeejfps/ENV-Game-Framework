namespace ZGF.Gui;

public class Container : Component
{
    private ILayout? _layout;
    public ILayout? Layout
    {
        get => _layout;
        set => SetField(ref _layout, value);
    }

    public Component? Child { get; set; }

    public override bool IsDirty
    {
        get
        {
            if (base.IsDirty)
                return true;

            if (Layout != null && Layout.IsDirty)
                return true;

            return false;
        }
    }

    protected override void OnLayoutSelf()
    {
        Console.WriteLine("Laying out container...");
        if (Layout == null)
            return;

        if (!Layout.IsDirty)
            return;

        Console.WriteLine("Asksing layout to do the layout");
        Position = Layout.DoLayout(Position);
        Console.WriteLine($"New position: {Position}");
    }

    protected override void OnApplyStyleSheet(StyleSheet styleSheet)
    {
        if (Layout == null)
            return;

        Layout.ApplyStyleSheet(styleSheet);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (Layout == null)
            return;

        Layout.DrawSelf(c);
    }
}