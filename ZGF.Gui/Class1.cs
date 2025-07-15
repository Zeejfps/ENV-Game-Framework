using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class EventSystem
{
    public static EventSystem Instance { get; } = new();

    public void AddMouseListener(Component component, IMouseListener mouseListener)
    {
        
    }

    public void Update()
    {
        
    }
}

public class Tester
{
    public void Run()
    {
        IGuiApp app = new App();
        var columnLayout = new ColumnLayout();
        columnLayout.Add(new Button());
        columnLayout.Add(new Button());
        columnLayout.Add(new Button());
        
        app.GuiContent.Layout = columnLayout;
        app.GuiContent.ApplyStyle(new StyleSheet());

        app.Run();
    }
}

public interface IGuiApp
{
    void Run();
    Container GuiContent { get; }
}

public sealed class App : IGuiApp
{
    public Container GuiContent { get; }

    public App()
    {
        GuiContent = new Container();
    }
    
    public void Run()
    {
        var renderer = new FakeRenderer();
        while (true)
        {
            GuiContent.LayoutSelf();
            GuiContent.RenderSelf(renderer);
        }
    }
}

public sealed class FakeRenderer : IRenderer
{
    public void DrawRect(RectF position, RectStyle style)
    {
        throw new NotImplementedException();
    }

    public void DrawText(RectF position, string text)
    {
        throw new NotImplementedException();
    }
}

public sealed class Button : Container, IMouseListener
{
    private Rect _background;
    
    private static RectStyle BackgroundHoveredStyle { get; } = new();
    private static RectStyle BackgroundNormalStyle { get; } = new();
    
    public Button()
    {
        _background = new Rect();
        
        var sackLayout = new StackLayout();
        sackLayout.Add(_background);
        sackLayout.Add(new Label("Hello World!"));
        Layout = sackLayout;

        AddMouseListener(this);
    }

    public void OnMouseEnter()
    {
        _background.Style = BackgroundHoveredStyle;
    }

    public void OnMouseExit()
    {
        _background.Style = BackgroundNormalStyle;
    }
}

public interface IMouseListener
{
    
}

public abstract class Layout : ILayout
{
    private readonly List<Component> _components = new();

    public bool IsDirty => _components.Any(component => component.IsDirty);
    
    public void Add(Component component)
    {
        _components.Add(component);
    }
    
    public RectF DoLayout(RectF position)
    {
        return OnDoLayout(position, _components);
    }

    public void ApplyStyleSheet(StyleSheet styleSheet)
    {
        foreach (var component in _components)
        {
            component.ApplyStyle(styleSheet);
        }
    }

    public void Render(IRenderer renderer)
    {
        foreach (var component in _components)
        {
            component.RenderSelf(renderer);
        }
    }

    protected abstract RectF OnDoLayout(RectF position, IReadOnlyList<Component> components);
}

public class StackLayout : Layout
{
    protected override RectF OnDoLayout(RectF position, IReadOnlyList<Component> components)
    {
        foreach (var component in components)
        {
            component.Position = position;
            component.LayoutSelf();
        }
        return position;
    }
}

public class Rect : Component
{
    private RectStyle _style;
    public RectStyle Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }
    
    protected override void OnRender(IRenderer r)
    {
        r.DrawRect(Position, Style);
    }
    
    protected override void OnApplyStyleSheet(StyleSheet styleSheet)
    {
        if (styleSheet.TryGetByClass(ClassId, out var style))
        {
            
        }
    }
}

public sealed class ColumnLayout : Layout
{
    protected override RectF OnDoLayout(RectF position, IReadOnlyList<Component> components)
    {
        var componentCount = components.Count;
        var componentHeight = position.Height / componentCount;
        foreach (var component in components)
        {
            component.Position = new RectF
            {
                Left = position.Left,
                Bottom = position.Bottom,
                Width = position.Width,
                Height = componentHeight,
            };
        }
        return position;
    }
}

public class Label : Component
{
    public Label(string text)
    {
        
    }

    protected override void OnRender(IRenderer r)
    {
        r.DrawText(Position, "Hello World");
    }
}

public class Container : Component
{
    private ILayout? _layout;
    public ILayout? Layout
    {
        get => _layout;
        set
        {
            if (SetField(ref _layout, value) && _layout != null)
            {
                _layout.ZIndex = ZIndex;
            }
        }
    }

    public override bool IsDirty
    {
        get
        {
            if (base.IsDirty)
                return true;

            if (Layout != null && Layout.IsDirty)
                return true;

            return false;
        }
    }
    
    protected override void OnLayout()
    {
        if (Layout == null)
            return;
        
        if (!Layout.IsDirty)
            return;
        
        Position = Layout.DoLayout(Position);
    }

    protected override void OnApplyStyleSheet(StyleSheet styleSheet)
    {
        if (Layout == null)
            return;
        
        Layout.ApplyStyleSheet(styleSheet);
    }

    protected override void OnRender(IRenderer r)
    {
        if (Layout == null)
            return;

        Layout.Render(r);
    }
}