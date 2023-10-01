using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using static GL46;

namespace OpenGLSandbox;

public sealed class GuiCommandBufferExperimentScene : IScene
{
    private readonly TextWidgetCommandBuffer m_ButtonText = new()
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
    
    public GuiCommandBufferExperimentScene(IWindow window, IInputSystem inputSystem)
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

    public void Update()
    {
        m_Button.IsHovered = m_Button.ScreenRect.Contains(InputSystem.Mouse.ScreenX, 640 - InputSystem.Mouse.ScreenY);
        m_Button.IsPressed = m_Button.IsHovered && InputSystem.Mouse.IsButtonPressed(MouseButton.Left);
        
        glClear(GL_COLOR_BUFFER_BIT);
        
        var commandBuffer = CommandBuffer;
        commandBuffer.Clear();
        
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
        PanelRenderPass.Unload();
        TextRenderer.Dispose();
    }
}

interface ICommandStorage
{
    void Clear();
}

class TextButton : WidgetCommandBuffer
{
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }

    private Color BackgroundNormalColor { get; } = Color.FromHex(0xffffff, 1f);
    private Color BackgroundPressedColor { get; } = Color.FromHex(0xfaf7fc, 1f);
    private Color BackgroundHoveredColor { get; } = Color.FromHex(0xfdfbfd, 1f);
    private Color TextNormalColor { get; } = Color.FromHex(0x1b1a1b, 1f);
    private Color TextPressedColor { get; } = Color.FromHex(0x5f5e60, 1f);
    private Color BorderColor { get; } = Color.FromHex(0xe8e2ea, 1f);
    
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

        ref var command = ref commandBuffer.AddDrawPanelCommand();
        command.BorderRadius = new Vector4(6f, 6f, 6f, 6f);
        command.BorderSize = BorderSize.All(1f);
        command.BorderColor = BorderColor;
        command.ScreenRect = ScreenRect;
        command.Color = backgroundColor;

        // commandBuffer.Add(new DrawTextCommand
        // {
        //     Style = new TextStyle
        //     {
        //         HorizontalTextAlignment = TextAlignment.Center,
        //         VerticalTextAlignment = TextAlignment.Center,
        //         Color = textColor
        //     },
        //     Text = "5",
        //     ScreenRect = ScreenRect
        // });
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

        var commands = commandBuffer.GetAllDrawTextCommands();
        //Console.WriteLine("Commands: " + commands.Length);
        foreach (var command in commands)
        {
            textRenderer.DrawText(command.ScreenRect, command.Style, command.Text);
        }
        
        textRenderer.Render();
    }
}

class CommandBuffer : ICommandBuffer
{
    private readonly DrawPanelCommand[] m_DrawPanelCommands = new DrawPanelCommand[20000];
    private readonly DrawTextCommand[] m_DrawTextCommands = new DrawTextCommand[20000];

    private int m_DrawPanelCommandCount;
    private int m_DrawTextCommandCount;
    
    public void Clear()
    {
        m_DrawPanelCommandCount = 0;
        m_DrawTextCommandCount = 0;
    }

    public ref DrawPanelCommand AddDrawPanelCommand()
    {
        return ref m_DrawPanelCommands[m_DrawPanelCommandCount++];
    }

    public ref DrawTextCommand AddDrawTextCommand()
    {
        return ref m_DrawTextCommands[m_DrawTextCommandCount++];
    }

    public ReadOnlySpan<DrawPanelCommand> GetAllDrawPanelCommands()
    {
        return m_DrawPanelCommands.AsSpan(0, m_DrawPanelCommandCount);
    }
    
    public ReadOnlySpan<DrawTextCommand> GetAllDrawTextCommands()
    {
        return m_DrawTextCommands.AsSpan(0, m_DrawTextCommandCount);
    }
}


public abstract class WidgetCommandBuffer
{
    public Rect ScreenRect { get; set; }
    public abstract void Render(ICommandBuffer commandBuffer);
}

public interface ICommandBuffer
{
    void Clear();

    public ref DrawPanelCommand AddDrawPanelCommand();
    public ref DrawTextCommand AddDrawTextCommand();
    ReadOnlySpan<DrawPanelCommand> GetAllDrawPanelCommands();
    ReadOnlySpan<DrawTextCommand> GetAllDrawTextCommands();
}

public sealed class TextWidgetCommandBuffer : WidgetCommandBuffer
{
    public string Text { get; set; } = string.Empty;

    public TextStyle Style { get; set; }

    public override void Render(ICommandBuffer commandBuffer)
    {
        ref var command = ref commandBuffer.AddDrawTextCommand();
        command.Style = Style;
        command.Text = Text;
        command.ScreenRect = ScreenRect;
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