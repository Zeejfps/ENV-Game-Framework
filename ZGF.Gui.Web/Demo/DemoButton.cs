using System.Runtime.Versioning;
using ZGF.Gui.Views;
using ZGF.Gui.Web.Input;

namespace ZGF.Gui.Web.Demo;

/// A clickable RectView + centered label whose background tracks pointer state.
/// The web host has no view-level input routing, so <see cref="Sync"/> polls
/// <see cref="WebInput"/> against the control's laid-out position each frame.
[SupportedOSPlatform("browser")]
internal sealed class DemoButton
{
    public RectView View { get; }
    public string Label { get; }
    public Action<DemoButton>? OnClick;

    private uint _base, _hover, _press;
    private readonly TextView _label;

    public DemoButton(string label, float height, float? width,
        uint baseColor, uint hoverColor, uint pressColor, uint textColor,
        float radius, bool center)
    {
        Label = label;
        _base = baseColor;
        _hover = hoverColor;
        _press = pressColor;

        _label = new TextView
        {
            Text = label,
            FontSize = 14,
            TextColor = textColor,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
        };

        View = new RectView
        {
            Height = height,
            BackgroundColor = baseColor,
            BorderRadius = BorderRadiusStyle.All(radius),
            Padding = center ? default : new PaddingStyle { Left = 12, Right = 12 },
            Children = { _label },
        };
        if (width.HasValue)
            View.Width = width.Value;
    }

    public void SetSelected(bool selected, uint baseColor, uint hoverColor, uint pressColor, uint textColor)
    {
        _base = baseColor;
        _hover = hoverColor;
        _press = pressColor;
        _label.TextColor = textColor;
    }

    public void Sync()
    {
        var over = WebInput.IsOver(View.Position);
        View.BackgroundColor = over
            ? (WebInput.IsButtonDown(0) ? _press : _hover)
            : _base;
    }
}
