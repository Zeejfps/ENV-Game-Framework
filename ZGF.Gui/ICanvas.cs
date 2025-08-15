using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void DrawRect(in DrawRectInputs inputs);
    void DrawText(in DrawTextInputs inputs);
    void DrawImage(in DrawImageInputs inputs);
    
    bool TryGetClip(out RectF rect);
    void PushClip(RectF rect);
    void PopClip();
    
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);
    float MeasureTextLineHeight(TextStyle style);

    Size GetImageSize(string imageId);
    int GetImageWidth(string imageId);
    int GetImageHeight(string imageId);
}