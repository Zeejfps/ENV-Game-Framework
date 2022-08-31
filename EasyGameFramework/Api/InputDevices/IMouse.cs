namespace EasyGameFramework.Api.InputDevices;

public interface IMouse
{
    public int ScreenX { get; set; }
    public int ScreenY { get; set; }

    public float ScrollDeltaX { get; set; }
    public float ScrollDeltaY { get; set; }

    public void PressButton(MouseButton button);
    public void ReleaseButton(MouseButton button);
    public bool WasButtonPressedThisFrame(MouseButton button);
    public bool WasButtonReleasedThisFrame(MouseButton button);
    public bool IsButtonPressed(MouseButton button);
    public bool IsButtonReleased(MouseButton button);

    void Update();
}