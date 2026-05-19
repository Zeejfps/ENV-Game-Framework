using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class AppBar : MultiChildView
{
    private readonly MenuItem _fileItem;
    private readonly MenuItem _editItem;
    private readonly MenuItem _viewLabel;
    private readonly MenuItem _helpLabel;

    public AppBar(App app)
    {
        _fileItem = new MenuItem
        {
            Text = "File"
        };

        _editItem = new MenuItem
        {
            Text = "Edit"
        };

        _viewLabel = new MenuItem
        {
            Text = "View"
        };

        var specialMenuItem = new MenuItem
        {
            Text = "Special",
            IsDisabled = true
        };

        _helpLabel = new MenuItem
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

        _fileItem.UseController(ctx => new FileMenuItemController(_fileItem, app, ctx));
        _editItem.UseController(ctx => new TestMenuItemController(_editItem, ctx));
        _viewLabel.UseController(ctx => new TestMenuItemController(_viewLabel, ctx));
        _helpLabel.UseController(ctx => new TestMenuItemController(_helpLabel, ctx));
    }
}