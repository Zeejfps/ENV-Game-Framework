using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class AppBar : View
{
    private readonly App _app;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly MenuItem _fileItem;
    private readonly MenuItem _editItem;
    private readonly MenuItem _viewLabel;
    private readonly MenuItem _helpLabel;

    public AppBar(App app, ContextMenuManager contextMenuManager)
    {
        _app = app;
        _contextMenuManager = contextMenuManager;

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
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        var inputSystem = context.Get<InputSystem>()!;
        inputSystem.RegisterController(_fileItem, new FileMenuItemController(_fileItem, _contextMenuManager, _app));
        inputSystem.RegisterController(_editItem, new TestMenuItemController(_editItem, _contextMenuManager));
        inputSystem.RegisterController(_viewLabel, new TestMenuItemController(_viewLabel, _contextMenuManager));
        inputSystem.RegisterController(_helpLabel, new TestMenuItemController(_helpLabel, _contextMenuManager));
    }
}