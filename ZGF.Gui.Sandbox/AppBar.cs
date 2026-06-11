using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Sandbox;

public sealed class AppBar : MultiChildView
{
    private readonly MenuItem _fileItem;
    private readonly MenuItem _editItem;
    private readonly MenuItem _viewLabel;
    private readonly MenuItem _helpLabel;

    public AppBar(App app, Context context)
    {
        var canvas = context.Canvas;

        _fileItem = new MenuItem(canvas)
        {
            Text = "File"
        };

        _editItem = new MenuItem(canvas)
        {
            Text = "Edit"
        };

        _viewLabel = new MenuItem(canvas)
        {
            Text = "View"
        };

        var specialMenuItem = new MenuItem(canvas)
        {
            Text = "Special",
            IsDisabled = true
        };

        _helpLabel = new MenuItem(canvas)
        {
            Text = "Help"
        };
        
        var row = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.Start,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Gap = 10,
            Children =
            {
                _fileItem,
                _editItem,
                _viewLabel,
                specialMenuItem,
                _helpLabel,
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
        
        var container = new RectView
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

        AddChildToSelf(container);

        var input = context.Require<InputSystem>();
        _fileItem.UseController(input, () => new FileMenuItemController(_fileItem, app, context));
        _editItem.UseController(input, () => new TestMenuItemController(_editItem, context));
        _viewLabel.UseController(input, () => new TestMenuItemController(_viewLabel, context));
        _helpLabel.UseController(input, () => new TestMenuItemController(_helpLabel, context));
    }
}