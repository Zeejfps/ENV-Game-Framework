using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class CheckboxView : HoverableButton
{
    private const float IconSize = 14f;

    public State<bool> IsChecked { get; } = new(false);

    private readonly State<uint> _idle = new(ThemePresets.Dark.Dialog.RowText);
    private readonly State<uint> _hover = new(ThemePresets.Dark.Dialog.RowTextActive);
    private readonly State<uint> _missing = new(ThemePresets.Dark.Dialog.RowTextMissing);

    public CheckboxView(string label)
    {
        var iconView = new TextView
        {
            FontFamily = LucideIcons.FontFamily,
            FontSize = IconSize,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindText(IsChecked, c => c ? LucideIcons.CheckSquare : LucideIcons.Square);
        iconView.BindTextColor(ComputeForeground);

        var labelView = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
        };
        labelView.BindTextColor(ComputeForeground);

        SetBackground(new FlexRowView
        {
            Gap = 6f,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { iconView, labelView },
        });

        this.BindToTheme(t =>
        {
            _idle.Value = t.Dialog.RowText;
            _hover.Value = t.Dialog.RowTextActive;
            _missing.Value = t.Dialog.RowTextMissing;
        });
    }

    protected override void OnClicked() => IsChecked.Value = !IsChecked.Value;

    private uint ComputeForeground()
    {
        if (!IsEnabled.Value) return _missing.Value;
        return IsHovered.Value ? _hover.Value : _idle.Value;
    }
}
