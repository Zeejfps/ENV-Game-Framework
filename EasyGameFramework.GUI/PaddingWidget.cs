﻿namespace OpenGLSandbox;

public sealed class PaddingWidget : Widget
{
    public Offsets Offsets { get; set; }
        
    public IWidget Child { get; set; }
        
    protected override IWidget Build(IBuildContext context)
    {
        var offset = Offsets;
        var myScreenRect = ScreenRect;
        myScreenRect.X += offset.Left;
        myScreenRect.Y += offset.Bottom;
        myScreenRect.Width -= offset.Left + offset.Right;
        myScreenRect.Height -= offset.Top + offset.Bottom;
        Child.ScreenRect = myScreenRect;
        return Child;
    }
}