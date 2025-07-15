using ZGF.Geometry;

namespace ZGF.Gui;

public interface ILayout
{
    RectF DoLayout(RectF position);
    void ApplyStyleSheet(StyleSheet styleSheet);
    void DrawSelf(ICanvas canvas);
    bool IsDirty { get; }
}