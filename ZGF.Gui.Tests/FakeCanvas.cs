using ZGF.Geometry;

namespace ZGF.Gui.Tests;

/// <summary>Inert canvas for layout/notify tests that need a TextView but never draw.</summary>
public sealed class FakeCanvas : ICanvas
{
    public void DrawRect(in DrawRectInputs inputs) { }
    public void DrawText(in DrawTextInputs inputs) { }
    public void DrawImage(in DrawImageInputs inputs) { }
    public void DrawBoxShadow(in DrawBoxShadowInputs inputs) { }
    public void DrawLine(in DrawLineInputs inputs) { }
    public void DrawCircle(in DrawCircleInputs inputs) { }
    public void DrawBezier(in DrawBezierInputs inputs) { }

    public bool TryGetClip(out RectF rect)
    {
        rect = default;
        return false;
    }

    public void PushClip(RectF rect) { }
    public void PopClip() { }

    public void PushOpacity(float opacity) { }
    public void PopOpacity() { }
    public void PushTranslation(float dx, float dy) { }
    public void PopTranslation() { }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style) => text.Length * 8f;
    public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) =>
        Math.Clamp(prefixLength, 0, text.Length) * 8f;
    public float MeasureTextLineHeight(TextStyle style) => 16f;

    public int GetImageWidth(string imageId) => 0;
    public int GetImageHeight(string imageId) => 0;
}
