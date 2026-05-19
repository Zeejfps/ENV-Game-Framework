using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

/// <summary>
/// Modal shown when a checkout attempt fails. Surfaces the git error message verbatim
/// (so it matches what the user would see running the command in a terminal) with a
/// single OK button.
/// </summary>
public sealed class CheckoutErrorDialog : MultiChildView
{
    private const float CloseButtonSize = 28f;

    public CheckoutErrorDialog(string message, Action onClose)
    {
        PreferredWidth = 460;
        PreferredHeight = 220;
        
        var title = new TextView
        {
            Text = "Checkout failed",
            TextColor = DialogPalette.TitleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var headerRow = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new MultiChildView { PreferredWidth = CloseButtonSize },
                new FlexItem { Grow = 1, Child = title },
                new DialogCloseButton(onClose),
            },
        };

        var messageView = new TextView
        {
            Text = message,
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var okButton = new DialogButton("OK", onClose)
        {
            PreferredHeight = 32,
        };

        var buttonsRow = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = new MultiChildView() },
                new FlexItem { Grow = 1, Child = okButton },
            },
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(10),
            Padding = PaddingStyle.All(20),
            Children =
            {
                new FlexColumnView
                {
                    Gap = 12,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        headerRow,
                        new RectView
                        {
                            BackgroundColor = DialogPalette.Separator,
                            PreferredHeight = 1,
                        },
                        new FlexItem { Grow = 1, Child = messageView },
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new ErrorDialogKbmController(onClose));
    }
}

internal sealed class ErrorDialogKbmController : KeyboardMouseController
{
    private readonly Action _onClose;

    public ErrorDialogKbmController(Action onClose)
    {
        _onClose = onClose;
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (e.Key == KeyboardKey.Escape
            || e.Key == KeyboardKey.Enter
            || e.Key == KeyboardKey.NumpadEnter)
        {
            e.Consume();
            _onClose();
        }
    }
}
