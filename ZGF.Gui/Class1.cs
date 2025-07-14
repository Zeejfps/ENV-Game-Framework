using System.Diagnostics.CodeAnalysis;
using ZGF.Geometry;

namespace ZGF.Gui;

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
            GuiContent.DoLayout();
            GuiContent.Render(renderer);
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
            component.Render(renderer);
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
            component.DoLayout();
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

public sealed class StyleSheet
{
    public bool TryGetById(string? id, [NotNullWhen(true)] out Style? style)
    {
        if (string.IsNullOrEmpty(id))
        {
            style = null;
            return false;
        }
        style = null;
        return false;
    }
    
    public bool TryGetByClass(string? classId, [NotNullWhen(true)] out Style? style)
    {
        if (string.IsNullOrEmpty(classId))
        {
            style = null;
            return false;
        }
        
        style = null;
        return false;
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

public sealed class Style
{
    public bool TryGetFloat(string name, out float value)
    {
        value = default;
        return false;
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

    protected override void OnRender(IRenderer r)
    {
        if (Layout == null)
            return;

        Layout.Render(r);
    }
}

public abstract class Component
{
    private RectF _position;
    public RectF Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }

    private string? _classId;
    public string? ClassId
    {
        get => _classId;
        set => SetField(ref _classId, value);
    }
    
    public virtual bool IsDirty { get; private set; }

    public void DoLayout()
    {
        OnLayout();
    }

    public void Render(IRenderer r)
    {
        OnRender(r);
    }

    public void ApplyStyle(StyleSheet styleSheet)
    {
        OnApplyStyleSheet(styleSheet);
    }
        
    public void AddMouseListener(IMouseListener mouseListener)
    {
        
    }

    protected bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        SetDirty();
        return true;
    }

    public void SetDirty()
    {
        IsDirty = true;
    }
    
    protected virtual void OnApplyStyleSheet(StyleSheet styleSheet){}
    protected virtual void OnLayout(){}
    protected virtual void OnRender(IRenderer r){}
}

public interface ILayout
{
    RectF DoLayout(RectF position);
    void ApplyStyleSheet(StyleSheet styleSheet);
    void Render(IRenderer renderer);
    bool IsDirty { get; }
}

public interface IRenderer
{
    void DrawRect(RectF position, RectStyle style);
    void DrawText(RectF position, string text);
}

public readonly struct RectStyle
{
    
}