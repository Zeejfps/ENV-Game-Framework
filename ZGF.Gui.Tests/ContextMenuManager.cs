namespace ZGF.Gui.Tests;

public interface IOpenedContextMenu
{
    bool IsOpened { get; }

    void KeepOpen();
    void Close();
}

sealed class OpenedContextMenu : IOpenedContextMenu
{
    public required ContextMenu ContextMenu { get; init; }
    public bool IsHovered { get; set; }
    public OpenedContextMenu? Parent { get; set; }
    public OpenedContextMenu? Child { get; set; }
    public long CloseTimestamp { get; set; }
    public bool IsOpened { get; set; }

    private readonly ContextMenuManager _contextMenuManager;

    public OpenedContextMenu(ContextMenuManager contextMenuManager)
    {
        _contextMenuManager = contextMenuManager;
    }

    public void KeepOpen()
    {
        _contextMenuManager.KeepOpen(this);
    }

    public void Close()
    {
        if (CanBeClosed())
        {
            _contextMenuManager.HideContextMenu(ContextMenu);
        }
    }

    public bool CanBeClosed()
    {
        Console.WriteLine("Can be closed?");
        if (IsHovered)
            return false;

        var parent = Parent;
        while (parent != null)
        {
            if (parent.IsHovered)
                return false;

            parent = parent.Parent;
        }

        var child = Child;
        while (child != null)
        {
            if (child.IsHovered)
                return false;

            child = child.Child;
        }

        return true;
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
            
            KeepOpen(openedParentMenu);
            if (openedParentMenu.Child != null && openedParentMenu.Child != openedMenu)
            {
                CloseMenu(openedParentMenu.Child);
            }

            openedParentMenu.Child = openedMenu;
            openedMenu.Parent = openedParentMenu;
        }

        _openedMenus[contextMenu] = openedMenu;
        _contextMenuPane.Add(contextMenu);
        return openedMenu;
    }
    
    public void OnMouseEnter(ContextMenu contextMenu)
    {
        Console.WriteLine($"Muse enter: {contextMenu.GetHashCode()}");
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            openedMenu.IsHovered = true;
            KeepOpen(openedMenu);
        }
    }

    public void OnMouseExit(ContextMenu contextMenu)
    {
        Console.WriteLine($"Mouse exit: {contextMenu.GetHashCode()}");
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            openedMenu.IsHovered = false;
            if (openedMenu.CanBeClosed())
            {
                CloseMenu(openedMenu);
            }
        }
    }

    public void HideContextMenu(ContextMenu contextMenu)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            CloseMenu(openedMenu);
        }
    }

    private void CloseMenu(OpenedContextMenu openedMenu)
    {
        Console.WriteLine($"Close menu: {openedMenu.GetHashCode()}");
        openedMenu.CloseTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _closingMenus.Add(openedMenu);
    }

    internal void KeepOpen(OpenedContextMenu openedMenu)
    {
        _closingMenus.Remove(openedMenu);
            
        var parent = openedMenu.Parent;
        while (parent != null)
        {
            _closingMenus.Remove(parent);
            parent = parent.Parent;
        }
            
        var child = openedMenu.Child;
        while (child != null)
        {
            _closingMenus.Remove(child);
            child = child.Child;
        }
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
                _contextMenuPane.Remove(component);
                _closingMenus.Remove(menu);
                _openedMenus.Remove(component);
                menu.IsOpened = false;
            }
        }
    }
}