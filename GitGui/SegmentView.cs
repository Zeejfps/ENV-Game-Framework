using ZGF.Gui;

namespace GitGui;

internal sealed class SegmentView : MultiChildView, IBind<SegmentViewModel>
{
    private const float SegmentHeight = 28f;
    private const uint IdleBg = 0x00000000u;

    private readonly RectView _bg;
    private readonly TextView _label;
    private ThemeTokens _tokens = ThemePresets.Dark;
    private bool _isActive;
    private bool _isHovered;

    private SegmentViewModel? _vm;

    public SegmentView(string label, BorderRadiusStyle cornerRadius)
    {
        PreferredHeight = SegmentHeight;

        _label = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _bg = new RectView
        {
            BackgroundColor = IdleBg,
            BorderRadius = cornerRadius,
            Padding = new PaddingStyle { Left = 12, Right = 12 },
            Children = { _label },
        };
        AddChildToSelf(_bg);

        this.BindToTheme(t => { _tokens = t; ApplyVisualState(); });
        this.UseController(_ => new HoverableButtonController(OnClicked, OnHoverChanged));
    }

    public void Bind(SegmentViewModel vm)
    {
        _vm = vm;
        vm.IsActive.Subscribe(SetActive);
    }

    private void SetActive(bool active)
    {
        if (_isActive == active) return;
        _isActive = active;
        ApplyVisualState();
    }

    private void OnClicked()
    {
        _vm?.Activate();
    }

    private void OnHoverChanged(bool hovered)
    {
        if (_isHovered == hovered) return;
        _isHovered = hovered;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        var d = _tokens.Dialog;
        if (_isActive)
        {
            _bg.BackgroundColor = d.RowActive;
            _label.TextColor = d.RowTextActive;
        }
        else if (_isHovered)
        {
            _bg.BackgroundColor = d.ButtonHover;
            _label.TextColor = d.RowTextActive;
        }
        else
        {
            _bg.BackgroundColor = IdleBg;
            _label.TextColor = d.RowText;
        }
    }
}
