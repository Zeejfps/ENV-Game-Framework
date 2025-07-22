namespace ZGF.Gui.Tests;

sealed class OpenedContextMenu
{
    public required ContextMenu ContextMenu { get; init; }
    public bool IsHovered { get; set; }
    public OpenedContextMenu? Parent { get; set; }
    public OpenedContextMenu? Child { get; set; }
    public long CloseTimestamp { get; set; }
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

    public void ShowContextMenu(ContextMenu contextMenu, ContextMenu? parentMenu = null)
    {
        if (_openedMenus.ContainsKey(contextMenu))
            return;

        var openedMenu = new OpenedContextMenu
        {
            ContextMenu = contextMenu,
            Child = null
        };
        
        if (parentMenu != null)
        {
            if (!_openedMenus.TryGetValue(parentMenu, out var openedParentMenu))
            {
                throw new Exception("Parent menu not opened");
            }
            
            if (openedParentMenu.Child != null)
            {
                CloseMenu(openedParentMenu.Child);
            }

            openedParentMenu.Child = openedMenu;
            openedMenu.Parent = openedParentMenu;
        }
        
        _openedMenus[contextMenu] = openedMenu;
        _contextMenuPane.Add(contextMenu);
        Console.WriteLine("Wtf");
    }
    
    public void OnMouseEnter(ContextMenu contextMenu)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            openedMenu.IsHovered = true;
            
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
    }

    public void OnMouseExit(ContextMenu contextMenu)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            openedMenu.IsHovered = false;
            if (CanBeClosed(openedMenu))
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
        openedMenu.CloseTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _closingMenus.Add(openedMenu);
    }

    private bool CanBeClosed(OpenedContextMenu openedMenu)
    {
        if (openedMenu.IsHovered)
            return false;
        
        var parent = openedMenu.Parent;
        while (parent != null)
        {
            if (parent.IsHovered)
                return false;
                
            parent = parent.Parent;
        }
        
        var child = openedMenu.Child;
        while (child != null)
        {
            if (child.IsHovered)
                return false;
                
            child = child.Child;
        }

        return true;
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
            }
        }
    }
}