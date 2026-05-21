using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

internal static class DialogFrame
{
    public const float CloseButtonSize = 28f;
    public const int DefaultPadding = 20;
    public const float DefaultBorderRadius = 10f;
    public const float DefaultButtonHeight = 32f;
    public const float DefaultButtonsGap = 8f;

    public static View Build(string title, Action onClose, FlexColumnView body)
    {
        body.Children.Insert(0, Separator());
        body.Children.Insert(0, Header(title, onClose));
        return Wrap(body);
    }

    public static FlexRowView Header(string title, Action onClose)
    {
        return new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new MultiChildView { PreferredWidth = CloseButtonSize },
                new FlexItem
                {
                    Grow = 1,
                    Child = new TextView
                    {
                        Text = title,
                        TextColor = DialogPalette.TitleText,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                    },
                },
                new DialogCloseButton(onClose),
            },
        };
    }

    public static RectView Separator() => new()
    {
        BackgroundColor = DialogPalette.Separator,
        PreferredHeight = 1,
    };

    public static FlexRowView ButtonsRow(MultiChildView cancel, MultiChildView primary, float gap = DefaultButtonsGap) => new()
    {
        Gap = gap,
        CrossAxisAlignment = CrossAxisAlignment.Stretch,
        Children =
        {
            new FlexItem { Grow = 1, Child = cancel },
            new FlexItem { Grow = 1, Child = primary },
        },
    };

    public static TextView ErrorView() => new()
    {
        Text = string.Empty,
        TextColor = 0xFFE06C75,
        TextWrap = TextWrap.Wrap,
    };

    public static TextInputView TextInput() => new()
    {
        BackgroundColor = DialogPalette.ButtonNormal,
        TextColor = DialogPalette.TitleText,
        CaretColor = DialogPalette.TitleText,
        SelectionRectColor = DialogPalette.RowActive,
        TextWrap = TextWrap.NoWrap,
    };

    public static RectView WrapInput(TextInputView input) => new()
    {
        BackgroundColor = DialogPalette.ButtonNormal,
        BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
        BorderSize = BorderSizeStyle.All(1),
        BorderRadius = BorderRadiusStyle.All(3),
        Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
        PreferredHeight = 28,
        Children = { input },
    };

    private static RectView Wrap(View child) => new()
    {
        BackgroundColor = DialogPalette.Background,
        BorderColor = BorderColorStyle.All(DialogPalette.Border),
        BorderSize = BorderSizeStyle.All(1),
        BorderRadius = BorderRadiusStyle.All(DefaultBorderRadius),
        Padding = PaddingStyle.All(DefaultPadding),
        Children = { child },
    };
}
