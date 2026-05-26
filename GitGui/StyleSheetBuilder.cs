using ZGF.Gui;

namespace GitGui;

/// <summary>
/// Translates <see cref="ThemeTokens"/> into a <see cref="StyleSheet"/>. One method per
/// area keeps the mapping readable and reviewable. Phase 2 only covers the three pilot
/// surfaces (dialog header, dialog button, tooltip); other areas are added in Phase 3.
/// </summary>
public static class StyleSheetBuilder
{
    public static StyleSheet Build(ThemeTokens tokens)
    {
        var sheet = new StyleSheet();
        BuildDialogRules(sheet, tokens);
        BuildTooltipRules(sheet, tokens.Tooltip);
        return sheet;
    }

    private static void BuildDialogRules(StyleSheet sheet, ThemeTokens tokens)
    {
        var dialog = tokens.Dialog;
        var text = tokens.Text;

        // Dialog header chrome — background panel + bottom border + side padding.
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogHeader),
            new Style
            {
                BackgroundColor = dialog.Background,
                BorderColor = new BorderColorStyle { Bottom = dialog.Border },
                BorderSize = new BorderSizeStyle { Bottom = 1 },
            });

        // Header text — bold and color-coded; the "detached" modifier dims to RowTextMissing.
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogHeaderIcon),
            new Style { TextColor = text.Strong });
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogHeaderIcon,
                modifiers: new[] { ModifierNames.Detached }),
            new Style { TextColor = dialog.RowTextMissing });

        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogHeaderPrefix),
            new Style { TextColor = text.Header });

        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogHeaderName),
            new Style { TextColor = text.Strong });
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogHeaderName,
                modifiers: new[] { ModifierNames.Detached }),
            new Style { TextColor = dialog.RowTextMissing });

        // Dialog button — chrome + hover/active state via modifiers.
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogButton),
            new Style
            {
                BackgroundColor = dialog.ButtonNormal,
                BorderColor = BorderColorStyle.All(dialog.ButtonBorder),
                BorderSize = BorderSizeStyle.All(1),
                BorderRadius = BorderRadiusStyle.All(6),
            });
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogButton,
                modifiers: new[] { ModifierNames.Hovered }),
            new Style
            {
                BackgroundColor = dialog.ButtonHover,
                BorderColor = BorderColorStyle.All(dialog.ButtonBorderHover),
            });

        // Button label / icon text color, dimmed when the button is in the disabled state.
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogButtonLabel),
            new Style { TextColor = text.Strong });
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogButtonLabel,
                modifiers: new[] { ModifierNames.Disabled }),
            new Style { TextColor = dialog.RowTextMissing });

        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogButtonIcon),
            new Style { TextColor = text.Strong });
        sheet.AddRule(
            new Selector(classId: StyleClassNames.DialogButtonIcon,
                modifiers: new[] { ModifierNames.Disabled }),
            new Style { TextColor = dialog.RowTextMissing });
    }

    private static void BuildTooltipRules(StyleSheet sheet, TooltipTokens tooltip)
    {
        sheet.AddRule(
            new Selector(classId: StyleClassNames.Tooltip),
            new Style
            {
                BackgroundColor = tooltip.Background,
                BorderColor = BorderColorStyle.All(tooltip.Border),
                BorderSize = BorderSizeStyle.All(1),
                BorderRadius = BorderRadiusStyle.All(4),
                BoxShadow = new BoxShadowStyle
                {
                    OffsetX = 0f,
                    OffsetY = -4f,
                    Blur = 16f,
                    Spread = 0f,
                    Color = tooltip.ShadowColor,
                },
            });

        sheet.AddRule(
            new Selector(classId: StyleClassNames.TooltipText),
            new Style
            {
                TextColor = tooltip.Text,
                FontSize = 12f,
                VerticalAlignment = TextAlignment.Center,
            });
    }
}
