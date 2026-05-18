using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class CheckboxView : HoverableButton
{
    private const float IconSize = 14f;

    public State<bool> IsChecked { get; } = new(false);

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
    }

    protected override void OnClicked() => IsChecked.Value = !IsChecked.Value;

    private uint ComputeForeground()
    {
        if (!IsEnabled.Value) return DialogPalette.RowTextMissing;
        return IsHovered.Value ? DialogPalette.RowTextActive : DialogPalette.RowText;
    }
}
