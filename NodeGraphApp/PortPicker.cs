namespace NodeGraphApp;

public sealed class PortPicker
{
    private readonly MousePicker _mousePicker;
    public InputPort? HoveredInputPort { get; private set; }
    
    public PortPicker(MousePicker mousePicker)
    {
        _mousePicker = mousePicker;
    }

    public void Update()
    {
        var hoveredNode = _mousePicker.HoveredNode;
        var hoveredPort = hoveredNode as InputPort;
        while (hoveredNode != null && hoveredPort == null && hoveredNode.Parent != null)
        {
            hoveredNode = hoveredNode.Parent;
            hoveredPort = hoveredNode as InputPort;
        }
        HoveredInputPort = hoveredPort;
    }
}