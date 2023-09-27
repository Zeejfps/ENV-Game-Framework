namespace OpenGLSandbox;

public sealed class GUIExperimentsSandbox : IScene
{
    private WidgetTree WidgetTree { get; } = new();
    
    public void Load()
    {
        var rootWidget = new TextWidget
        {
            ScreenRect = new Rect(0, 0, 640f, 640f),
            Text = "Hello World!"
        };

        WidgetTree.Root = rootWidget;
    }

    public void Render()
    {
    }

    public void Unload()
    {
    }
}

public class WidgetTree
{
    public IWidget Root { get; set; }
}

public interface IWidget
{
    
}

public abstract class Widget : IWidget
{
    public Rect ScreenRect { get; set; }
}

public interface ITextWidgetFactory
{
    ITextWidget Create();
}

public interface ICommandBuffer
{
    void DrawRect(Rect screenRect, RectStyle style);
    void DrawText(Rect screenRect, string text, TextStyle style);
}

public interface ITextWidget
{
    
}

public sealed class TextWidget : Widget, ITextWidget
{
    public string Text { get; set; } = string.Empty;
    public TextStyle TextStyle { get; set; }

    private ICommandBuffer CommandBuffer { get; }
    
    public void Render()
    {
        CommandBuffer.DrawRect(ScreenRect, new RectStyle());
        CommandBuffer.DrawText(ScreenRect, Text, TextStyle);
    }
}

public struct DrawRectCommand
{
    public Rect ScreenRect;
    public RectStyle Style;
}

public struct DrawTextCommand
{
    public Rect ScreenRect;
    public ReadOnlyMemory<char> Text;
    public TextStyle Style;
}

public struct RectStyle
{
    
}

public sealed class CommandBufferImpl : ICommandBuffer
{
    private DrawRectCommand[] m_DrawRectCommands = new DrawRectCommand[100];
    private int m_DrawRectCommandCount;
    
    public void BeginFrame()
    {
        m_DrawRectCommandCount = 0;
    }
    
    public void DrawRect(Rect screenRect, RectStyle style)
    {
        ref var command = ref m_DrawRectCommands[m_DrawRectCommandCount];
        command.ScreenRect = screenRect;
        command.Style = style;
        m_DrawRectCommandCount++;
    }

    public void DrawText(Rect screenRect, string text, TextStyle style)
    {
    }

    public void EndFrame()
    {
        
    }
}