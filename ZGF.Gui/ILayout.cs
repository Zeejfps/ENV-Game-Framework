using ZGF.Geometry;

namespace ZGF.Gui;

public interface ILayout
{
    RectF DoLayout(RectF position);
    void ApplyStyleSheet(StyleSheet styleSheet);
    void Render(IRenderer renderer);
    bool IsDirty { get; }
}