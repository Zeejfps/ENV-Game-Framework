using ZGF.Geometry;

namespace ZGF.Gui.Tests;

readonly record struct DrawCommand2(int Sequence, int ZIndex, RectF ClippingRect, CommandKind Kind);

public sealed class OpenGlRenderedCanvas : ICanvas
{
    private readonly SortedSet<DrawCommand> _commands = new();
    private readonly Dictionary<int, DrawRectInputs> _rectCommandData = new();
    private readonly Dictionary<int, DrawTextInputs> _textCommandData = new();
    private readonly Dictionary<int, DrawImageInputs> _imageCommandData = new();

    
    public void BeginFrame()
    {
        
    }

    public void EndFrame()
    {
        
    }
    
    public void DrawRect(in DrawRectInputs inputs)
    {
        throw new NotImplementedException();
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        throw new NotImplementedException();
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        throw new NotImplementedException();
    }

    public bool TryGetClip(out RectF rect)
    {
        throw new NotImplementedException();
    }

    public void PushClip(RectF rect)
    {
        throw new NotImplementedException();
    }

    public void PopClip()
    {
        throw new NotImplementedException();
    }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
    {
        throw new NotImplementedException();
    }

    public float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style)
    {
        throw new NotImplementedException();
    }

    public Size GetImageSize(string imageId)
    {
        throw new NotImplementedException();
    }

    public int GetImageWidth(string imageId)
    {
        throw new NotImplementedException();
    }

    public int GetImageHeight(string imageId)
    {
        throw new NotImplementedException();
    }
}