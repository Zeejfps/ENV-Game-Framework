using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public sealed class PaddingWidget : Widget
{
    public Offsets Offsets { get; set; }
        
    public IWidget? Child { get; set; }
        
    protected override IWidget BuildContent(IBuildContext context)
    {
        return Child;
    }

    public override void Layout(IBuildContext context)
    {
        var offset = Offsets;
        var myScreenRect = ScreenRect;
        myScreenRect.X += offset.Left;
        myScreenRect.Y += offset.Bottom;
        myScreenRect.Width -= offset.Left + offset.Right;
        myScreenRect.Height -= offset.Top + offset.Bottom;
        ScreenRect = myScreenRect;
        
        base.Layout(context);
    }

    public override Rect Measure(IBuildContext context)
    {
        var myScreenRect = base.Measure(context);
        var offset = Offsets;
        myScreenRect.X -= offset.Left;
        myScreenRect.Y -= offset.Bottom;
        myScreenRect.Width += offset.Left + offset.Right;
        myScreenRect.Height += offset.Top + offset.Bottom;
        return myScreenRect;
    }
}