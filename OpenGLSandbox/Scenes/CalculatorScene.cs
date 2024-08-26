using System.Numerics;
using System.Text;
using EasyGameFramework.Api;
using static GL46;

namespace OpenGLSandbox;

public sealed class CalculatorScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    
    private readonly PanelRenderer m_PanelRenderer;
    private readonly BitmapFontTextRenderer m_TextRenderer;

    private IBuildContext m_Context;
    private IWidget m_CalculatorWidget;

    public CalculatorScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
        m_PanelRenderer = new PanelRenderer(window);
        m_TextRenderer = new BitmapFontTextRenderer(window);
    }

    public void Load()
    {
        m_Window.SetScreenSize(400, 640);
        m_Window.Title = "Calculator";
        //m_Window.IsResizable = false;

        m_PanelRenderer.Load();
        m_TextRenderer.Load(new []
        {
            new BmpFontFile
            {
                FontName = "Segoe UI",
                PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
            },
            new BmpFontFile
            {
                FontName = "Segoe UI Symbols",
                PathToFile = "Assets/bitmapfonts/Segoe UI Symbols.fnt"
            }
        });
        
        var bgColor = Color.FromHex(0x221e26, 1f);
        glClearColor(bgColor.R, bgColor.G, bgColor.B, bgColor.A);
        
        m_Context = new TestContext(m_PanelRenderer, m_TextRenderer, m_InputSystem, m_Window);
        m_CalculatorWidget = new CalculatorWidget
        {
            ScreenRect = new Rect(0, 0, m_Window.ScreenWidth, m_Window.ScreenHeight)
        };
    }

    public void Update()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        
        m_CalculatorWidget.Update(m_Context);
        
        m_PanelRenderer.Update();
        m_TextRenderer.Update();
    }

    class TestContext : IBuildContext
    {
        public IPanelRenderer PanelRenderer { get; }
        public ITextRenderer TextRenderer { get; }
        public IWindow Window { get; }
        public IInputSystem InputSystem { get; }
        public FocusTree FocusTree { get; }

        public TestContext(IPanelRenderer panelRenderer, ITextRenderer textRenderer, IInputSystem inputSystem, IWindow window)
        {
            PanelRenderer = panelRenderer;
            TextRenderer = textRenderer;
            InputSystem = inputSystem;
            Window = window;
            FocusTree = new FocusTree(inputSystem, window);
        }
    }

    public void Unload()
    {
        m_PanelRenderer.Unload();
        m_TextRenderer.Unload();
    }

    sealed class CalculatorTextButtonWidget : StatefulWidget
    {
        public PanelStyle PanelStyleNormal { get; } = new()
        {
            BackgroundColor = Color.FromHex(0x403743, 1f),
            BorderRadius = new Vector4(7f, 7f, 7f, 7f),
            BorderColor = Color.FromHex(0x322e35, 1f),
            BorderSize = BorderSize.All(1f)
        };
        
        public PanelStyle PanelStyleHovered { get; } = new()
        {
            BackgroundColor = Color.FromHex(0x382e3c, 1f),
            BorderRadius = new Vector4(7f, 7f, 7f, 7f),
            BorderColor = Color.FromHex(0x332e35, 1f),
            BorderSize = BorderSize.All(1f)
        };
    
        private readonly PanelStyle m_PanelStylePressed = new()
        {
            BackgroundColor = Color.FromHex(0x2a262e, 1f),
            BorderRadius = new Vector4(7f, 7f, 7f, 7f),
            BorderColor = Color.FromHex(0x332e35, 1f),
            BorderSize = BorderSize.All(1f)
        };
            
        private readonly TextStyle m_TextStyleNormal = new()
        {
            Color = Color.FromHex(0xf7f7f7, 1f),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
    
        private readonly TextStyle m_TextStylePressed = new()
        {
            Color = Color.FromHex(0xc8c7c9, 1f),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        
        private bool m_IsHovered;
        private bool IsHovered
        {
            get => m_IsHovered;
            set => SetField(ref m_IsHovered, value);
        }
        
        private bool m_IsPressed;

        private bool IsPressed
        {
            get => m_IsPressed;
            set => SetField(ref m_IsPressed, value);
        }

        private string Text { get; set; }
        private readonly InputListenerController m_InputListenerController = new();
        
        public CalculatorTextButtonWidget(string text)
        {
            Text = text;
        }

        protected override IWidget Build(IBuildContext context)
        {
            //Console.WriteLine("Build:TextButtonWidget");

            var panelStyle = PanelStyleNormal;
            var textStyle = m_TextStyleNormal;
            if (IsPressed)
            {
                panelStyle = m_PanelStylePressed;
                textStyle = m_TextStylePressed;
            }
            else if (IsHovered)
            {
                panelStyle = PanelStyleHovered;
            }

            return new InputListenerWidget(m_InputListenerController)
            {
                ScreenRect = ScreenRect,
                OnPointerEnter = () => IsHovered = true,
                OnPointerExit = () => IsHovered = false,
                OnPointerPressed = () => IsPressed = true,
                OnPointerReleased = () => IsPressed = false,
                Child = new StackWidget
                {
                    Children = new List<IWidget>
                    {
                        new PanelWidget
                        {
                            Style = panelStyle
                        },
                        new TextWidget(Text)
                        {
                            FontFamily = "Segoe UI",
                            Style = textStyle
                        }    
                    }
                }
            };
        }
    }

    sealed class CalculatorWidget : Widget
    {
        private PanelStyle PanelStyleNormal { get; } = new()
        {
            BackgroundColor = Color.FromHex(0x3e3940, 1f),
            BorderRadius = new Vector4(7f, 7f, 7f, 7f),
            BorderColor = Color.FromHex(0x322e35, 1f),
            BorderSize = BorderSize.All(1f)
        };
        
        private PanelStyle PanelStyleHovered { get; } = new()
        {
            BackgroundColor = Color.FromHex(0x382e3c, 1f),
            BorderRadius = new Vector4(7f, 7f, 7f, 7f),
            BorderColor = Color.FromHex(0x332e35, 1f),
            BorderSize = BorderSize.All(1f)
        };
    
        private readonly PanelStyle m_PanelStylePressed = new()
        {
            BackgroundColor = Color.FromHex(0x2a262e, 1f),
            BorderRadius = new Vector4(7f, 7f, 7f, 7f),
            BorderColor = Color.FromHex(0x332e35, 1f),
            BorderSize = BorderSize.All(1f)
        };
            
        private readonly TextStyle m_TextStyleNormal = new()
        {
            Color = Color.FromHex(0xf7f7f7, 1f),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
    
        private readonly TextStyle m_TextStylePressed = new()
        {
            Color = Color.FromHex(0xc8c7c9, 1f),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        private InputListenerController m_InputListenerController = new();
        
        protected override IWidget Build(IBuildContext context)
        {
            var x = "\U0001D465";

            return new InputListenerWidget(m_InputListenerController)
            {
                ScreenRect = ScreenRect,
                OnPointerPressed = () => Console.WriteLine("Wooo Pressed"),
                Child = new PaddingWidget
                {
                    //ScreenRect = new Rect(0, 0, 400f, 410f),
                    Offsets = Offsets.All(6f),
                    Child = new GridWidget
                    {
                        ColumnCount = 4,
                        RowCount = 6,
                        Spacing = 2f,
                        Children = new List<IWidget>
                        {
                            new CalculatorTextButtonWidget("+/-"),
                            new CalculatorTextButtonWidget("0"),
                            new CalculatorTextButtonWidget("."),
                            new CalculatorTextButtonWidget("="),
                            
                            new CalculatorTextButtonWidget("1"),
                            new CalculatorTextButtonWidget("2"),
                            new CalculatorTextButtonWidget("3"),
                            new CalculatorTextButtonWidget("+"),
                            
                            new CalculatorTextButtonWidget("4"),
                            new CalculatorTextButtonWidget("5"),
                            new CalculatorTextButtonWidget("6"),
                            new CalculatorTextButtonWidget("-"),
                            
                            new CalculatorTextButtonWidget("7"),
                            new CalculatorTextButtonWidget("8"),
                            new CalculatorTextButtonWidget("9"),
                            new CalculatorTextButtonWidget("x"),
                            
                            new CalculatorTextButtonWidget("⅟" + x),
                            new CalculatorTextButtonWidget(x + "²"),
                            new CalculatorTextButtonWidget("\u221A" + x),
                            new CalculatorTextButtonWidget("\u00F7"),
                            
                            new CalculatorTextButtonWidget("%"),
                            new CalculatorTextButtonWidget("CE"),
                            new CalculatorTextButtonWidget("C"),
                            new CalculatorTextButtonWidget(((char)57475).ToString()),
                        }
                    }
                }
            };
        }
    }

    sealed class GridWidget : Widget
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public float Spacing { get; set; }
        
        public List<IWidget> Children { get; set; }
        
        protected override IWidget Build(IBuildContext context)
        {
            var availableWidth = ScreenRect.Width - (ColumnCount - 1) * Spacing;
            var availableHeight = ScreenRect.Height - (RowCount - 1) * Spacing;
            var cellWidth = availableWidth / ColumnCount;
            var cellHeight = availableHeight / RowCount;
            var xOffset = ScreenRect.X;
            var yOffset = ScreenRect.Y;
            
            for (var i = 0; i < RowCount; i++)
            {
                for (var j = 0; j < ColumnCount; j++)
                {
                    var childIndex = j + i * ColumnCount;
                    if (childIndex >= Children.Count)
                        break;

                    Children[childIndex].ScreenRect = new Rect(j * cellWidth + xOffset + (j*Spacing), i * cellHeight + i * Spacing + yOffset, cellWidth, cellHeight);
                }
            }

            return new MultiChildWidget(Children);
        }
    }
}