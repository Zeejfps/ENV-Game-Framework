namespace ZGF.Gui.Tests;

public sealed class TextButton : Component
{
    private Panel _background;

    private static Style BackgroundHoveredStyle { get; } = new();
    private static Style BackgroundNormalStyle { get; } = new();

    public TextButton(string text)
    {
        _background = new Panel
        {
            BackgroundColor = 0x232345,
            BorderSize = BorderSizeStyle.All(4),
        };

        Add(_background);
        Add(new Label
        {
            Text = text
        });

        IsInteractable = true;
    }

    protected override void OnMouseEnter()
    {
        _background.ApplyStyle(BackgroundHoveredStyle);
        Context?.MouseInputSystem.TryFocus(this);
    }

    protected override void OnMouseExit()
    {
        _background.ApplyStyle(BackgroundNormalStyle);
        Context?.MouseInputSystem.Blur(this);
    }
}