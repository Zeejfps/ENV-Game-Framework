using ZGF.Geometry;

namespace ZGF.Gui;

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

    private int _zIndex;
    public int ZIndex
    {
        get => _zIndex;
        set => SetField(ref _zIndex, value);
    }
    
    public virtual bool IsDirty { get; private set; }

    public void LayoutSelf()
    {
        OnLayout();
    }

    public void DrawSelf(ICanvas r)
    {
        OnDraw(r);
    }

    public void ApplyStyle(StyleSheet styleSheet)
    {
        OnApplyStyleSheet(styleSheet);
    }
        
    public void AddMouseListener(IMouseListener mouseListener)
    {
        EventSystem.Instance.AddMouseListener(this, mouseListener);
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
    protected virtual void OnDraw(ICanvas c){}
}