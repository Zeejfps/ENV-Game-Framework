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
        HoveredInputPort = _mousePicker.HoveredNode as InputPort;
    }
}