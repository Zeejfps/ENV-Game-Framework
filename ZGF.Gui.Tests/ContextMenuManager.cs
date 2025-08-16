namespace ZGF.Gui.Tests;

public interface IOpenedContextMenu
{
    event Action Closed;
    View View { get; }
    bool IsOpened { get; }

    void CancelCloseRequest();
    void CloseRequest();
}

sealed class OpenedContextMenu : IOpenedContextMenu
{
    public event Action? Closed;

    public bool IsOpened { get; private set; } = true;
    public required ContextMenu ContextMenu { get; init; }
    public OpenedContextMenu? Parent { get; set; }
    public OpenedContextMenu? Child { get; set; }
    public long CloseTimestamp { get; set; }
    public View View => ContextMenu;

    public bool IsCloseRequested { get; private set; }

    public void CancelCloseRequest()
    {
        if (!IsOpened)
            return;

        IsCloseRequested = false;
    }

    public void CloseRequest()
    {
        IsCloseRequested = true;
    }

    public void Close()
    {
        IsOpened = false;
        Closed?.Invoke();
    }
}

public sealed class ContextMenuManager
{
    private readonly View _contextMenuPane;
    
    private readonly HashSet<OpenedContextMenu> _closingMenus = new();
    private readonly Dictionary<ContextMenu, OpenedContextMenu> _openedMenus = new();
    
    public ContextMenuManager(View contextMenuPane)
    {
        _contextMenuPane = contextMenuPane;
    }

    public IOpenedContextMenu? ShowContextMenu(ContextMenu contextMenu, ContextMenu? parentMenu = null)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            throw new Exception("Menu already opened");
        }

        openedMenu = new OpenedContextMenu
        {
            ContextMenu = contextMenu,
        };
        
        if (parentMenu != null)
        {
            if (!_openedMenus.TryGetValue(parentMenu, out var openedParentMenu))
            {
                return null;
            }
            openedParentMenu.Child = openedMenu;
            openedMenu.Parent = openedParentMenu;
        }

        _openedMenus[contextMenu] = openedMenu;
        _contextMenuPane.Children.Add(contextMenu);
        return openedMenu;
    }

    public void Update()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var menu in _closingMenus.ToList())
        {
            var component = menu.ContextMenu;
            var timestamp = menu.CloseTimestamp;
            if (now - timestamp > 100)
            {
                Console.WriteLine($"Closing: {menu.GetHashCode()}");
                _contextMenuPane.Children.Remove(component);
                _closingMenus.Remove(menu);
                _openedMenus.Remove(component);
                menu.Close();
            }
        }

        foreach (var contextMenu in _openedMenus.Values.ToList())
        {
            if (contextMenu.IsCloseRequested && contextMenu.Child == null)
            {
                contextMenu.CloseTimestamp = now;
                _closingMenus.Add(contextMenu);
                _openedMenus.Remove(contextMenu.ContextMenu);

                var parent = contextMenu.Parent;
                if (parent != null)
                {
                    parent.Child = null;
                }
            }
        }
    }

    public void RequestCloseMenu(ContextMenu contextMenu)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            openedMenu.CloseRequest();
        }
    }
}