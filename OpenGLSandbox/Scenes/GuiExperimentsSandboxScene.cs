using System.Numerics;
using static GL46;

namespace OpenGLSandbox;

public sealed class GuiExperimentsSandboxScene : IScene
{
    private readonly ContainerWidget m_Button = new()
    {
        ScreenRect = new Rect(40, 20, 100, 50),
        BorderSize = BorderSize.FromTRBL(10, 10, 10, 10),
        BorderRadius = new Vector4(5, 5, 5, 5)
    };
    
    private readonly TextWidget m_ButtonText = new()
    {
        ScreenRect = new Rect(20, 20, 200, 50),
        Text = "Hello World!",
        Style = new TextStyle
        {
            Color = Color.FromHex(0xFF00FF, 1f),
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        }
    };

    private ICommandBuffer CommandBuffer { get; } = new CommandBuffer();
    private TextRenderPass TextRenderPass { get; }
    private TextRenderer TextRenderer { get; }
    
    public GuiExperimentsSandboxScene()
    {
        TextRenderer = new TextRenderer();
        TextRenderPass = new TextRenderPass(TextRenderer);
    }
    
    public void Load()
    {
        TextRenderer.Load();
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        
        var commandBuffer = CommandBuffer;
        commandBuffer.Clear();
        
        m_Button.Render(commandBuffer);
        m_ButtonText.Render(commandBuffer);

        TextRenderPass.Execute(commandBuffer);
    }

    public void Unload()
    {
        TextRenderer.Dispose();
    }
}

interface ICommandStorage
{
    void Clear();
}

class TextRenderPass
{
    public TextRenderPass(TextRenderer textRenderer)
    {
        TextRenderer = textRenderer;
    }

    private TextRenderer TextRenderer { get; }
    
    public void Execute(ICommandBuffer commandBuffer)
    {
        var textRenderer = TextRenderer;
        textRenderer.Clear();
        
        var commands = commandBuffer.GetAll<DrawTextCommand>();
        //Console.WriteLine("Commands: " + commands.Length);
        foreach (var command in commands)
        {
            textRenderer.DrawText(command.ScreenRect, command.Style, command.Text);
        }
        
        textRenderer.Render();
    }
}

class CommandStorage<T> : ICommandStorage
{
    private readonly T[] m_Buffer;
    private int m_Count;

    public CommandStorage(int size)
    {
        m_Buffer = new T[size];
    }
    
    public void Clear()
    {
        m_Count = 0;
    }

    public void Add(T command)
    {
        //Console.WriteLine($"Added command: {typeof(T)}");
        m_Buffer[m_Count] = command;
        m_Count++;
    }

    public ReadOnlySpan<T> GetAll()
    {
        return m_Buffer.AsSpan(0, m_Count);
    }
}

class CommandBuffer : ICommandBuffer
{
    private Dictionary<Type, ICommandStorage> TypeToStorageTable { get; } = new();
    
    public void Clear()
    {
        foreach (var storage in TypeToStorageTable.Values)
            storage.Clear();
    }

    public void Add<T>(T command)
    {
        var commandType = typeof(T);
        if (!TypeToStorageTable.TryGetValue(commandType, out var storage))
        {
            storage = new CommandStorage<T>(512);
            TypeToStorageTable[commandType] = storage;
        }

        ((CommandStorage<T>)storage).Add(command);
    }

    public ReadOnlySpan<T> GetAll<T>()
    {
        var commandType = typeof(T);
        if (TypeToStorageTable.TryGetValue(commandType, out var storage))
        {
            return ((CommandStorage<T>)storage).GetAll();
        }
        return ReadOnlySpan<T>.Empty;
    }
}

public interface IWidget
{
    
}

public sealed class ContainerWidget : Widget
{
    public BorderSize BorderSize;
    public Vector4 BorderRadius;

    public void Render(ICommandBuffer commandBuffer)
    {
        commandBuffer.Add(new DrawRectCommand
        {
            ScreenRect = ScreenRect,
            BorderRadius = BorderRadius,
            BorderSize = BorderSize
        });
    }
}

public abstract class Widget : IWidget
{
    public Rect ScreenRect { get; set; }
}

public interface ICommandBuffer
{
    void Clear();
    void Add<T>(T command);
    ReadOnlySpan<T> GetAll<T>();
}

public sealed class TextWidget : Widget
{
    public string Text { get; set; } = string.Empty;

    public TextStyle Style { get; set; }

    public void Render(ICommandBuffer commandBuffer)
    {
        commandBuffer.Add(new DrawTextCommand
        {
            Style = Style,
            Text = Text,
            ScreenRect = ScreenRect
        });
    }
}

public struct DrawRectCommand
{
    public Rect ScreenRect;
    public BorderSize BorderSize;
    public Vector4 BorderRadius;
}

public struct DrawTextCommand
{
    public Rect ScreenRect;
    public string Text;
    public TextStyle Style;
}