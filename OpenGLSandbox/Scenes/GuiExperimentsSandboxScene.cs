using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using static GL46;

namespace OpenGLSandbox;

public sealed class GuiExperimentsSandboxScene : IScene
{
    private readonly ContainerWidget m_Container = new()
    {
        Color = Color.FromHex(0x24ff55, 1f),
        ScreenRect = new Rect(20, 20, 200, 50),
        BorderSize = BorderSize.FromTRBL(0, 0, 0, 0),
        BorderRadius = new Vector4(5f, 5f, 5f, 5f),
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

    private readonly TextButton m_Button = new TextButton
    {
        ScreenRect = new Rect(200f, 300f, 96, 63f),
    };

    private TextButton[] m_TestButtons;
    
    private ICommandBuffer CommandBuffer { get; } = new CommandBuffer();
    private TextRenderPass TextRenderPass { get; }
    private PanelRenderPass PanelRenderPass { get; }
    private TextRenderer TextRenderer { get; }

    public IWindow Window { get; }
    private IInputSystem InputSystem { get; }
    
    public GuiExperimentsSandboxScene(IWindow window, IInputSystem inputSystem)
    {
        TextRenderer = new TextRenderer();
        PanelRenderPass = new PanelRenderPass(window);
        TextRenderPass = new TextRenderPass(TextRenderer);
        Window = window;
        InputSystem = inputSystem;

        m_TestButtons = new TextButton[50 * 50];
        for (var i = 0; i < 50; i++)
        {
            for (var j = 0; j < 50; j++)
            {
                var size = 12.8f;
                m_TestButtons[i * 50 + j] = new TextButton
                {
                    ScreenRect = new Rect(i*size, j*size, size, size)
                };
            }
        }
        
    }
    
    public void Load()
    {
        PanelRenderPass.Load();
        TextRenderer.Load();
        var bg = Color.FromHex(0xf7f0f9, 1f);
        glClearColor(bg.R, bg.G, bg.B, bg.A);
    }

    public void Render()
    {
        m_Button.IsHovered = m_Button.ScreenRect.Contains(InputSystem.Mouse.ScreenX, 640 - InputSystem.Mouse.ScreenY);
        m_Button.IsPressed = m_Button.IsHovered && InputSystem.Mouse.IsButtonPressed(MouseButton.Left);
        
        glClear(GL_COLOR_BUFFER_BIT);
        
        var commandBuffer = CommandBuffer;
        commandBuffer.Clear();
        
        //m_Container.Render(commandBuffer);
        m_ButtonText.Render(commandBuffer);
        m_Button.Render(commandBuffer);

        foreach (var testButton in m_TestButtons)
        {
            testButton.Render(commandBuffer);
        }
        
        PanelRenderPass.Execute(commandBuffer);
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

class TextButton : Widget
{
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }

    private Color BackgroundNormalColor { get; } = Color.FromHex(0xffffff, 1f);
    private Color BackgroundPressedColor { get; } = Color.FromHex(0xfaf7fc, 1f);
    private Color BackgroundHoveredColor { get; } = Color.FromHex(0xfdfbfd, 1f);
    private Color TextNormalColor { get; } = Color.FromHex(0x1b1a1b, 1f);
    private Color TextPressedColor { get; } = Color.FromHex(0x5f5e60, 1f);
    
    public override void Render(ICommandBuffer commandBuffer)
    {
        var backgroundColor = BackgroundNormalColor;
        var textColor = TextNormalColor;
        if (IsPressed)
        {
            backgroundColor = BackgroundPressedColor;
            textColor = TextPressedColor;
        }
        else if (IsHovered)
        {
            backgroundColor = BackgroundHoveredColor;
        }
        
        commandBuffer.Add(new DrawPanelCommand
        {
            BorderRadius = new Vector4(6f, 6f, 6f, 6f),
            BorderSize = BorderSize.All(1f),
            BorderColor = Color.FromHex(0xe8e2ea, 1f),
            ScreenRect = ScreenRect,
            Color = backgroundColor
        });
     
        commandBuffer.Add(new DrawTextCommand
        {
            Style = new TextStyle
            {
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Color = textColor
            },
            Text = "5",
            ScreenRect = ScreenRect
        });
    }
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
            storage = new CommandStorage<T>(20000);
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
    void Render(ICommandBuffer commandBuffer);
}

public sealed class ContainerWidget : Widget
{
    public Color Color;
    public BorderSize BorderSize;
    public Vector4 BorderRadius;

    public override void Render(ICommandBuffer commandBuffer)
    {
        commandBuffer.Add(new DrawPanelCommand
        {
            ScreenRect = ScreenRect,
            BorderRadius = BorderRadius,
            BorderSize = BorderSize,
            Color = Color,
        });
    }
}

public abstract class Widget : IWidget
{
    public Rect ScreenRect { get; set; }
    public abstract void Render(ICommandBuffer commandBuffer);
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

    public override void Render(ICommandBuffer commandBuffer)
    {
        commandBuffer.Add(new DrawTextCommand
        {
            Style = Style,
            Text = Text,
            ScreenRect = ScreenRect
        });
    }
}

public struct DrawPanelCommand
{
    public Rect ScreenRect;
    public BorderSize BorderSize;
    public Vector4 BorderRadius;
    public Color Color;
    public Color BorderColor;
}

public struct DrawTextCommand
{
    public Rect ScreenRect;
    public string Text;
    public TextStyle Style;
}