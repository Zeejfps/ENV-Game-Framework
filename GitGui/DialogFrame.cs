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
        var titleView = new TextView
        {
            Text = title,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        titleView.BindTextColorFromTheme(t => t.Dialog.TitleText);

        return new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new MultiChildView { PreferredWidth = CloseButtonSize },
                new FlexItem { Grow = 1, Child = titleView },
                new DialogCloseButton(onClose),
            },
        };
    }

    public static RectView Separator()
    {
        var view = new RectView { PreferredHeight = 1 };
        view.BindBackgroundColorFromTheme(t => t.Dialog.Separator);
        return view;
    }

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

    public static TextInputView TextInput()
    {
        var input = new TextInputView { TextWrap = TextWrap.NoWrap };
        input.BindToTheme(t =>
        {
            input.BackgroundColor = t.Dialog.ButtonNormal;
            input.TextColor = t.Dialog.TitleText;
            input.CaretColor = t.Dialog.TitleText;
            input.SelectionRectColor = t.Dialog.RowActive;
        });
        return input;
    }

    public static RectView WrapInput(TextInputView input)
    {
        var wrap = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            PreferredHeight = 28,
            Children = { input },
        };
        wrap.BindBackgroundColorFromTheme(t => t.Dialog.ButtonNormal);
        wrap.BindBorderColorFromTheme(t => BorderColorStyle.All(t.Dialog.ButtonBorder));
        return wrap;
    }

    private static RectView Wrap(View child)
    {
        var wrap = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(DefaultBorderRadius),
            Padding = PaddingStyle.All(DefaultPadding),
            Children = { child },
        };
        wrap.BindBackgroundColorFromTheme(t => t.Dialog.Background);
        wrap.BindBorderColorFromTheme(t => BorderColorStyle.All(t.Dialog.Border));
        return wrap;
    }
}
