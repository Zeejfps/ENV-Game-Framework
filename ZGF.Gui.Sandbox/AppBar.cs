using ZGF.Gui.Bindings;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Sandbox;

public sealed record AppBar : Widget
{
    protected override View CreateView(Context ctx)
    {
        var canvas = ctx.Canvas;
        var input = ctx.Require<InputSystem>();
        var app = ctx.Require<App>();

        View MenuItem(string text, bool isDisabled, Func<View, State<bool>, BaseMenuItemController>? createController)
        {
            var isSelected = new State<bool>(false);
            var textView = new TextView(canvas)
            {
                Text = text,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = isDisabled ? 0xFF959595 : 0xFF000000,
            };
            var item = new RectView
            {
                BackgroundColor = 0xFFDEDEDE,
                Padding = PaddingStyle.All(3),
                Children = { textView },
            };
            item.BindBackgroundColor(() => isSelected.Value ? 0x9C9CCEu : 0xFFDEDEDEu);
            if (createController != null)
                item.UseController(input, () => createController(item, isSelected));
            return item;
        }

        var row = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.Start,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Gap = 10,
            Children =
            {
                MenuItem("File", false, (item, selected) => new FileMenuItemController(item, selected, app, ctx)),
                MenuItem("Edit", false, (item, selected) => new TestMenuItemController(item, selected, ctx)),
                MenuItem("View", false, (item, selected) => new TestMenuItemController(item, selected, ctx)),
                MenuItem("Special", true, null),
                MenuItem("Help", false, (item, selected) => new TestMenuItemController(item, selected, ctx)),
            }
        };

        var background = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Padding = PaddingStyle.All(6),
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFFFF,
                Left = 0xFFFFFFFF,
                Right = 0xFF9C9C9C,
                Bottom = 0xFF9C9C9C
            },
            Children =
            {
                row
            }
        };

        return new RectView
        {
            BackgroundColor = 0xFF000000,
            Padding = new PaddingStyle
            {
                Bottom = 1,
            },
            Children =
            {
                background
            }
        };
    }
}
