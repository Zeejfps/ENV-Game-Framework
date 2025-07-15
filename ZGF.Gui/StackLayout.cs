namespace ZGF.Gui;

public class StackLayout : MultiChildComponent
{
    protected override void OnLayoutSelf()
    {
        base.OnLayoutSelf();
        var position = Position;
        foreach (var component in Children)
        {
            component.Position = position;
            component.LayoutSelf();
        }
    }
}