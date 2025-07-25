namespace ZGF.Gui.Tests;

public sealed class ModelView : View
{
    public string? ImageId { get; set; }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (ImageId == null)
            return;
        
        c.AddCommand(new DrawImageCommand
        {
            Position = Position,
            ZIndex = ZIndex,
            ImageId = ImageId
        });
    }
}