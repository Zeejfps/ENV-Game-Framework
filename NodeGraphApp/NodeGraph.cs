using System.Numerics;

namespace NodeGraphApp;

public sealed class NodeGraph
{
    private Link? _hoveredLink;
    public Link? HoveredLink
    {
        get => _hoveredLink;
        set
        {
            if (_hoveredLink == value)
                return;

            var prevHoveredLink = _hoveredLink;
            _hoveredLink = value;

            if (prevHoveredLink != null)
                prevHoveredLink.IsHovered = false;

            if (_hoveredLink != null)
                _hoveredLink.IsHovered = true;
        }
    }

    private OutputPort? _hoveredOutputPort;
    public OutputPort? HoveredOutputPort
    {
        get => _hoveredOutputPort;
        set => SetHoveredPort(ref _hoveredOutputPort, value);
    }

    private InputPort? _hoveredInputPort;
    public InputPort? HoveredInputPort
    {
        get => _hoveredInputPort;
        set => SetHoveredPort(ref _hoveredInputPort, value);
    }

    private void SetHoveredPort<T>(ref T? port, T? value) where T : class, IPort
    {
        if (port == value)
            return;
        var prevHoveredPort = port;
        port = value;

        if (prevHoveredPort != null)
            prevHoveredPort.IsHovered = false;

        if (port != null)
            port.IsHovered = true;
    }
    
    private Node? _hoveredNode;
    public Node? HoveredNode
    {
        get => _hoveredNode;
        set
        {
            if (_hoveredNode == value)
                return;
            var prevHoveredNode = _hoveredNode;
            _hoveredNode = value;
            if (prevHoveredNode != null)
                prevHoveredNode.IsHovered = false;
            if (_hoveredNode != null)
                _hoveredNode.IsHovered = true;
        }
    }
    
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

    private List<Node>? _copiedNodes;
    private List<Link>? _copiedLinks;
    
    public void Copy()
    {
        if (!_selectedNodes.Any() && !_selectedLinks.Any())
            return;
            
        _copiedNodes = _selectedNodes.ToList();
        _copiedLinks = _selectedLinks.ToList();
    }
    
    public void Paste(Vector2 position)
    {
        if (_copiedNodes == null || !_copiedNodes.Any())
            return;
    
        ClearSelectedNodes();
        
        var offset = position - _copiedNodes[0].Position;
        foreach (var originalNode in _copiedNodes)
        {
            var clone = originalNode.Clone();
            clone.Position = originalNode.Position + offset;
            Nodes.Add(clone);
            SelectNode(clone);
        }
    }
}