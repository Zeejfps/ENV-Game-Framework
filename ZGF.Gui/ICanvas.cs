using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void DrawRect(in DrawRectInputs inputs);
    void DrawText(in DrawTextInputs inputs);
    void DrawImage(in DrawImageInputs inputs);
    void DrawBoxShadow(in DrawBoxShadowInputs inputs);
    void DrawLine(in DrawLineInputs inputs);
    void DrawCircle(in DrawCircleInputs inputs);
    
    bool TryGetClip(out RectF rect);
    void PushClip(RectF rect);
    void PopClip();
    
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);
    float MeasureTextLineHeight(TextStyle style);

    int GetImageWidth(string imageId);
    int GetImageHeight(string imageId);
}