namespace ZGF.Gui;

public class Rect : Component
{
    private RectStyle _style;
    public RectStyle Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }

    protected override void OnDraw(ICanvas c)
    {
        c.DrawRect(Position, Style);
    }

    protected override void OnApplyStyleSheet(StyleSheet styleSheet)
    {
        if (styleSheet.TryGetByClass(ClassId, out var style))
        {

        }
    }
}