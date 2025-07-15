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
        var canvas = new FakeCanvas();
        while (true)
        {
            canvas.BeginFrame();
            GuiContent.LayoutSelf();
            GuiContent.DrawSelf(canvas);
            canvas.EndFrame();
            EventSystem.Instance.Update();
        }
    }
}



public sealed class FakeCanvas : ICanvas
{
    private VisualTree _visualTree = new();

    public void BeginFrame()
    {

    }

    public void DrawRect(RectF position, RectStyle style)
    {
        _visualTree.AddRect(position, style);
    }

    public void DrawText(RectF position, string text, TextStyle style)
    {
        _visualTree.AddText(position, text, style);
    }

    public void EndFrame()
    {

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
    public int ZIndex { get; set; }

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

    public void DrawSelf(ICanvas canvas)
    {
        foreach (var component in _components)
        {
            component.DrawSelf(canvas);
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

public class Container : Component
{
    private ILayout? _layout;
    public ILayout? Layout
    {
        get => _layout;
        set => SetField(ref _layout, value);
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

    protected override void OnDraw(ICanvas c)
    {
        if (Layout == null)
            return;

        Layout.DrawSelf(c);
    }
}