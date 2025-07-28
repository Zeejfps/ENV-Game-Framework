using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void AddCommand(in DrawRectCommand command);
    void AddCommand(in DrawTextCommand command);
    void AddCommand(in DrawImageCommand command);
    
    bool TryGetClip(out RectF rect);
    void PushClip(RectF rect);
    void PopClip();
    
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);
    float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style);
}