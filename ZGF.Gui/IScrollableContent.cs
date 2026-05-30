namespace ZGF.Gui;

/// <summary>
/// Surface a scrollable view exposes so an external sync controller can mirror its scroll
/// state into stand-alone scroll bars. Implemented by <see cref="ScrollPane"/> and by
/// <c>DiffContentView</c> in GitGui.
/// </summary>
public interface IScrollableContent
{
    event Action<float>? VerticalScrollPositionChanged;
    event Action<float>? HorizontalScrollPositionChanged;

    float VerticalScale { get; }
    float HorizontalScale { get; }

    void SetVerticalNormalizedScrollPosition(float normalized);
    void SetHorizontalNormalizedScrollPosition(float normalized);
}
