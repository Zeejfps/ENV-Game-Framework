using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void DrawRect(in DrawRectCommand command);
    void DrawText(in DrawTextCommand command);
    void DrawImage(in DrawImageCommand command);
    
    bool TryGetClip(out RectF rect);
    void PushClip(RectF rect);
    void PopClip();
    
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);
    float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style);
}