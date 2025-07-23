namespace ZGF.Gui.Tests;

public interface IOpenedContextMenu
{
    Component Component { get; }
    bool IsOpened { get; }

    void CancelCloseRequest();
    void Close();
}

sealed class OpenedContextMenu : IOpenedContextMenu
{
    public required ContextMenu ContextMenu { get; init; }
    public OpenedContextMenu? Parent { get; set; }
    public OpenedContextMenu? Child { get; set; }
    public long CloseTimestamp { get; set; }
    public bool IsOpened { get; set; }
    public Component Component => ContextMenu;

    public bool IsCloseRequested { get; private set; }

    private readonly ContextMenuManager _contextMenuManager;

    public OpenedContextMenu(ContextMenuManager contextMenuManager)
    {
        _contextMenuManager = contextMenuManager;
    }

    public void CancelCloseRequest()
    {
        Console.WriteLine($"CancelCloseRequest: {GetHashCode()}");
        IsCloseRequested = false;
        _contextMenuManager.KeepOpen(this);
    }

    public void Close()
    {
        Console.WriteLine($"Close requested: {GetHashCode()}");
        IsCloseRequested = true;
    }
}

public sealed class ContextMenuManager
{
    private readonly Component _contextMenuPane;
    
    private readonly HashSet<OpenedContextMenu> _closingMenus = new();
    private readonly Dictionary<ContextMenu, OpenedContextMenu> _openedMenus = new();
    
    public ContextMenuManager(Component contextMenuPane)
    {
        _contextMenuPane = contextMenuPane;
    }

    public IOpenedContextMenu ShowContextMenu(ContextMenu contextMenu, ContextMenu? parentMenu = null)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            throw new Exception("Menu already opened");
        }

        openedMenu = new OpenedContextMenu(this)
        {
            ContextMenu = contextMenu,
            IsOpened = true,
        };
        
        if (parentMenu != null)
        {
            if (!_openedMenus.TryGetValue(parentMenu, out var openedParentMenu))
            {
                throw new Exception("Parent menu not opened");
            }
            openedParentMenu.Child = openedMenu;
            openedMenu.Parent = openedParentMenu;
        }

        _openedMenus[contextMenu] = openedMenu;
        _contextMenuPane.Add(contextMenu);
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
                _contextMenuPane.Remove(component);
                _closingMenus.Remove(menu);
                _openedMenus.Remove(component);
                menu.IsOpened = false;
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
                while (parent != null)
                {
                    if (parent.IsCloseRequested)
                    {
                        parent.CloseTimestamp = now;
                        _closingMenus.Add(parent);
                    }
                    parent = parent.Parent;
                }
            }
        }
    }

    internal void KeepOpen(OpenedContextMenu openedContextMenu)
    {
        _closingMenus.Remove(openedContextMenu);
        var parent =  openedContextMenu.Parent;
        while (parent != null)
        {
            _closingMenus.Remove(parent);
            parent = parent.Parent;
        }
    }
}