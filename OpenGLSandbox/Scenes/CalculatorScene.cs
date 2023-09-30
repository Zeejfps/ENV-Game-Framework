using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using static GL46;

namespace OpenGLSandbox;

public sealed class CalculatorScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    
    private readonly PanelRenderer m_PanelRenderer;
    private readonly BitmapFontTextRenderer m_TextRenderer;
    
    private TextButtonOld? m_TextButton;

    private readonly List<WidgetOld> m_Widgets = new();

    private readonly PanelStyle m_ButtonStyleNormal = new()
    {
        BackgroundColor = Color.FromHex(0x3e3940, 1f),
        BorderRadius = new Vector4(7f, 7f, 7f, 7f),
        BorderColor = Color.FromHex(0x322e35, 1f),
        BorderSize = BorderSize.All(1f)
    };
    
    private readonly PanelStyle m_ButtonStylePressed = new()
    {
        BackgroundColor = Color.FromHex(0x2a262e, 1f),
        BorderRadius = new Vector4(7f, 7f, 7f, 7f),
        BorderColor = Color.FromHex(0x332e35, 1f),
        BorderSize = BorderSize.All(1f)
    };

    private readonly TextStyle m_ButtonTextStyleNormal = new()
    {
        Color = Color.FromHex(0xf7f7f7, 1f),
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalTextAlignment = TextAlignment.Center
    };
    
    private readonly TextStyle m_ButtonTextStylePressed = new()
    {
        Color = Color.FromHex(0xc8c7c9, 1f),
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalTextAlignment = TextAlignment.Center
    };

    private TextButtonWidget test;
    private IBuildContext context;
    
    public CalculatorScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
        m_PanelRenderer = new PanelRenderer(window);
        m_TextRenderer = new BitmapFontTextRenderer(window, "Assets/bitmapfonts/Segoe UI.fnt");

        context = new TestContext(m_PanelRenderer, m_TextRenderer, inputSystem, window);
        test = new TextButtonWidget
        {
            ScreenRect = new Rect(50, 50, 200, 50)
        };
    }

    class TestContext : IBuildContext
    {
        private IPanelRenderer PanelRenderer { get; }
        private ITextRenderer TextRenderer { get; }
        
        private IWindow Window { get; }
        private IInputSystem InputSystem { get; }

        public TestContext(IPanelRenderer panelRenderer, ITextRenderer textRenderer, IInputSystem inputSystem, IWindow window)
        {
            PanelRenderer = panelRenderer;
            TextRenderer = textRenderer;
            InputSystem = inputSystem;
            Window = window;
        }
        
        public T Get<T>()
        {
            if (typeof(T) == typeof(ITextRenderer))
                return (T)TextRenderer;
            if (typeof(T) == typeof(IPanelRenderer))
                return (T)PanelRenderer;
            if (typeof(T) == typeof(IWindow))
                return (T)Window;
            if (typeof(T) == typeof(IInputSystem))
                return (T)InputSystem;
            
            return default;
        }
    }

    public void Load()
    {
        m_Window.SetScreenSize(400, 640);
        m_Window.Title = "Calculator";

        m_PanelRenderer.Load();
        m_TextRenderer.Load();
        
        // m_TextButton = new TextButtonOld(m_PanelRenderer, m_TextRenderer)
        // {
        //     ScreenRect = new Rect(10f, 10f, 200f, 60f),
        //     Text = "+/-",
        //     PanelStyleNormal = m_ButtonStyleNormal,
        //     PanelStylePressed = m_ButtonStylePressed,
        //     TextStyleNormal = m_ButtonTextStyleNormal,
        //     TextStylePressed = m_ButtonTextStylePressed,
        //     IsVisible = true
        // };
        //m_Widgets.Add(m_TextButton);

        var bgColor = Color.FromHex(0x221e26, 1f);
        glClearColor(bgColor.R, bgColor.G, bgColor.B, bgColor.A);
    }

    public void Update()
    {
        // var mouse = m_InputSystem.Mouse;
        // var mouseScreenPosition = new Vector2(mouse.ScreenX, m_Window.ScreenHeight - mouse.ScreenY);
        // foreach (var widget in m_Widgets)
        // {
        //     widget.IsHovered = widget.ScreenRect.Contains(mouseScreenPosition);
        //     widget.IsPressed = widget.IsHovered && mouse.IsButtonPressed(MouseButton.Left);
        //     widget.Update();
        // }
        
        glClear(GL_COLOR_BUFFER_BIT);
        
        test.Update(context);
        
        m_PanelRenderer.Update();
        m_TextRenderer.Update();
    }

    public void Unload()
    {
        m_Widgets.Clear();
        m_PanelRenderer.Unload();
        m_TextRenderer.Unload();
    }

    interface IWidget : IDisposable
    {
        Rect ScreenRect { get; set; }
        void Update(IBuildContext context);
    }
    
    abstract class Widget : IWidget
    {
        public Rect ScreenRect { get; set; }
        
        private IWidget? m_Content;
        
        public virtual void Update(IBuildContext context)
        {
            m_Content ??= Build(context);
            if (m_Content != null && m_Content != this)
                m_Content.Update(context);
        }

        public virtual void Dispose()
        {
            m_Content?.Dispose();
            m_Content = null;
        }

        protected abstract IWidget Build(IBuildContext context);
    }

    abstract class StatefulWidget : Widget
    {
        private bool m_IsDirty;
        
        public sealed override void Update(IBuildContext context)
        {
            if (m_IsDirty)
            {
                m_IsDirty = false;
                Dispose();
            }
            base.Update(context);
        }

        protected void SetDirty()
        {
            m_IsDirty = true;
        }
    }
    
    sealed class TextWidget : Widget
    {
        public string Text { get; }
        public TextStyle Style { get; init; }
        
        private IRenderedText? m_RenderedText;

        public TextWidget(string text)
        {
            Text = text;
        }
        
        protected override IWidget Build(IBuildContext context)
        {
            Console.WriteLine("Build:TextWidget");
            var renderer = context.Get<ITextRenderer>();
            m_RenderedText = renderer.Render(Text, ScreenRect, Style);
            return this;
        }

        public override void Dispose()
        {
            m_RenderedText?.Dispose();
        }
    }

    sealed class MultiChildWidget : IWidget
    {
        private IEnumerable<IWidget> m_Children;

        public Rect ScreenRect { get; set; }

        public MultiChildWidget(IEnumerable<IWidget> children)
        {
            m_Children = children;
        }

        public void Update(IBuildContext context)
        {
            foreach (var child in m_Children)
                child.Update(context);
        }

        public void Dispose()
        {
            foreach (var child in m_Children)
                child.Dispose();
        }
    }

    sealed class StackWidget : Widget
    {
        public List<IWidget> Children { get; init; } = new();
        

        protected override IWidget Build(IBuildContext context)
        {
            Console.WriteLine("Build:StackWidget");
            foreach (var widget in Children)
            {
                widget.ScreenRect = ScreenRect;
            }
            return new MultiChildWidget(Children);
        }
    }

    sealed class PanelWidget : Widget
    {
        public PanelStyle Style { get; init; }

        private IRenderedPanel? m_RenderedPanel;
        
        protected override IWidget Build(IBuildContext context)
        {
            Console.WriteLine("Build:PanelWidget");
            var renderer = context.Get<IPanelRenderer>();
            m_RenderedPanel = renderer.Render(ScreenRect, Style);
            return this;
        }

        public override void Dispose()
        {
            m_RenderedPanel?.Dispose();
            m_RenderedPanel = null;
        }
    }

    sealed class InputHandlerWidget : Widget
    {
        public Action? OnPointerEnter { get; set; }
        public Action? OnPointerExit { get; set; }
        
        public IWidget Child { get; init; }

        private bool m_IsPointerHovering;

        private bool IsPointerHovering
        {
            get => m_IsPointerHovering;
            set
            {
                if (m_IsPointerHovering == value)
                    return;
                m_IsPointerHovering = value;
                if (m_IsPointerHovering)
                    OnPointerEnter?.Invoke();
                else
                    OnPointerExit?.Invoke();
            }
        }

        private int ScreenHeight;
        private IMouse? m_Mouse;

        protected override IWidget Build(IBuildContext context)
        {
            Console.WriteLine("Build:InputHandlerWidget");
            
            var window = context.Get<IWindow>();
            ScreenHeight = window.ScreenHeight;
            
            var inputSystem = context.Get<IInputSystem>();
            m_Mouse = inputSystem.Mouse;
            m_Mouse.Moved += Mouse_OnMoved;
            
            Child.ScreenRect = ScreenRect;

            m_IsPointerHovering = ScreenRect.Contains(m_Mouse.ScreenX, ScreenHeight - m_Mouse.ScreenY);
            
            return Child;
        }

        public override void Dispose()
        {
            if (m_Mouse != null)
                m_Mouse.Moved -= Mouse_OnMoved;
            base.Dispose();
        }

        private void Mouse_OnMoved(in MouseMovedEvent evt)
        {
            var mouse = evt.Mouse;
            IsPointerHovering = ScreenRect.Contains(mouse.ScreenX, ScreenHeight - mouse.ScreenY);
        }
    }

    interface IBuildContext
    {
        T Get<T>();
    }
    
    sealed class TextButtonWidget : StatefulWidget
    {
        private bool m_IsHovered;

        private bool IsHovered
        {
            get => m_IsHovered;
            set
            {
                if (m_IsHovered == value)
                    return;
                m_IsHovered = value;
                SetDirty();
            }
        }
        
        protected override IWidget Build(IBuildContext context)
        {
            Console.WriteLine("Build:TextButtonWidget");
            
            var panelBackgroundColor = Color.FromHex(0xff00ff, 0f);
            if (IsHovered)
            {
                panelBackgroundColor = Color.FromHex(0x11ff22, 1f);
            }

            return new InputHandlerWidget
            {
                OnPointerEnter = () =>
                {
                    IsHovered = true;
                },
                
                OnPointerExit = () =>
                {
                    IsHovered = false;
                },

                ScreenRect = ScreenRect,
                Child = new StackWidget
                {
                    Children = new List<IWidget>
                    {
                        new PanelWidget
                        {
                            Style = new PanelStyle
                            {
                                BackgroundColor = panelBackgroundColor,
                                BorderRadius = new Vector4(6f, 6f, 6f, 6f),
                                BorderColor = Color.FromHex(0xff00ff, 1f),
                                BorderSize = BorderSize.All(2f)
                            }
                        },
                        new TextWidget("Hi")
                        {
                            Style = new TextStyle
                            {
                                Color = Color.FromHex(0xff00ff, 1f),
                                HorizontalTextAlignment = TextAlignment.Center,
                                VerticalTextAlignment = TextAlignment.Center
                            }
                        }    
                    }
                }
            };
        }
    }

    abstract class WidgetOld
    {
        private Rect m_ScreenRect;
        public Rect ScreenRect
        {
            get => m_ScreenRect;
            set => SetField(ref m_ScreenRect, value);
        }
        
        private bool m_IsVisible;
        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (m_IsVisible == value)
                    return;
                
                m_IsVisible = value;
                OnVisibilityStateChanged();
            }
        }

        protected virtual void OnVisibilityStateChanged()
        {
            if (m_IsVisible)
                OnBecameVisible();
            else
                OnBecameHidden();
            SetDirty();
        }
        
        protected abstract void OnBecameVisible();
        protected abstract void OnBecameHidden();

        private bool m_IsHovered;
        public bool IsHovered
        {
            get => m_IsHovered;
            set => SetField(ref m_IsHovered, value);
        }
        
        private bool m_IsPressed;
        public bool IsPressed
        {
            get => m_IsPressed;
            set => SetField(ref m_IsPressed, value);
        }
        
        private bool m_IsDirty;
        public void Update()
        {
            if (m_IsDirty && m_IsVisible)
                UpdateView();
            m_IsDirty = false;
        }

        protected bool SetField<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            SetDirty();
            return true;
        }
        
        protected void SetDirty()
        {
            m_IsDirty = true;
        }

        protected abstract void UpdateView();
    }
    
    sealed class TextButtonOld : WidgetOld
    {
        private string m_Text;
        public string Text
        {
            get => m_Text;
            set => SetField(ref m_Text, value);
        }

        private TextStyle m_TextStyleNormal;
        public TextStyle TextStyleNormal
        {
            get => m_TextStyleNormal;
            set => SetField(ref m_TextStyleNormal, value);
        }

        private TextStyle m_TextStylePressedPressed;
        public TextStyle TextStylePressed
        {
            get => m_TextStylePressedPressed;
            set => SetField(ref m_TextStylePressedPressed, value);
        }

        private PanelStyle m_PanelStyleNormal;
        public PanelStyle PanelStyleNormal
        {
            get => m_PanelStyleNormal;
            set => SetField(ref m_PanelStyleNormal, value);
        }
        
        private PanelStyle m_PanelStylePressed;
        public PanelStyle PanelStylePressed
        {
            get => m_PanelStylePressed;
            set => SetField(ref m_PanelStylePressed, value);
        }
        
        private readonly ITextRenderer m_TextRenderer;
        private readonly IPanelRenderer m_PanelRenderer;
        
        private IRenderedPanel? m_RenderedPanel;
        private IRenderedText? m_RenderedText;

        public TextButtonOld(IPanelRenderer panelRenderer, ITextRenderer textRenderer)
        {
            m_PanelRenderer = panelRenderer;
            m_TextRenderer = textRenderer;
        }

        protected override void OnBecameVisible()
        {
            m_RenderedPanel = m_PanelRenderer.Render(ScreenRect, PanelStyleNormal);
            m_RenderedText = m_TextRenderer.Render(Text, ScreenRect, TextStyleNormal);
        }

        protected override void OnBecameHidden()
        {
            m_RenderedPanel?.Dispose();
            m_RenderedPanel = null;
            
            m_RenderedText?.Dispose();
            m_RenderedText = null;
        }

        protected override void UpdateView()
        {
            Debug.Assert(m_RenderedPanel != null);
            Debug.Assert(m_RenderedText != null);

            var panelStyle = PanelStyleNormal;
            var textStyle = TextStyleNormal;
            if (IsPressed)
            {
                textStyle = TextStylePressed;
                panelStyle = PanelStylePressed;
            }
            else if (IsHovered)
            {
            }
            
            m_RenderedPanel.ScreenRect = ScreenRect;
            m_RenderedPanel.Style = panelStyle;
            
            m_RenderedText.ScreenRect = ScreenRect;
            m_RenderedText.Style = textStyle;
        }
    }
}

