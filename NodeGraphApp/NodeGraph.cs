namespace NodeGraphApp;

public sealed class NodeGraph
{
    public NodeManager Nodes { get; } = new();
    public LinksRepo BackgroundLinks { get; } = new();
    public LinksRepo ForegroundLinks { get; } = new();
    public ConnectionManager Connections { get; } = new();
    public SelectionBox SelectionBox { get; } = new();

        
    private readonly HashSet<Node> _selectedNodes = new();
    private readonly HashSet<Link> _selectedLinks = new();

    public IEnumerable<Node> SelectedNodes => _selectedNodes;
    public IEnumerable<Link> SelectedLinks => _selectedLinks;

    public bool SelectNode(Node node)
    {
        if (_selectedNodes.Add(node))
        {
            node.IsSelected = true;
            return true;
        }

        return false;
    }

    public bool DeselectNode(Node node)
    {
        if (_selectedNodes.Remove(node))
        {
            node.IsSelected = false;
            return true;
        }
        return false;
    }
    
    public bool SelectLink(Link link)
    {
        if (_selectedLinks.Add(link))
        {
            link.IsSelected = true;
            return true;
        }

        return false;
    }
    
    public bool DeselectLink(Link link)
    {
        if (_selectedLinks.Remove(link))
        {
            link.IsSelected = false;
            return true;
        }
        return false;
    }
    
    public bool IsSelected(Node node)
    {
        return _selectedNodes.Contains(node);
    }
    
    public bool IsSelected(Link link)
    {
        return _selectedLinks.Contains(link);
    }
    
    public void ClearSelectedLinks()
    {
        foreach (var link in _selectedLinks)
        {
            link.IsSelected = false;
        }
        _selectedLinks.Clear();;
    }
    
    public void ClearSelectedNodes()
    {
        foreach (var selectedNode in _selectedNodes)
            selectedNode.IsSelected = false;
        _selectedNodes.Clear();
    }
    
    public void Update()
    {
        foreach (var node in Nodes.GetAll())
        {
            node.Update();
        }
        
        var links = BackgroundLinks.GetAll()
            .Concat(ForegroundLinks.GetAll());
        foreach (var link in links)
        {
            if (Connections.TryGetOutputPortForLink(link, out var outputPort))
            {
                link.StartPosition = outputPort.Socket.CenterPosition;
            }
            if (Connections.TryGetInputPortForLink(link, out var inputPort))
            {
                link.EndPosition = inputPort.Socket.CenterPosition;
            }
        }
    }
}