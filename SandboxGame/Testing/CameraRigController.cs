using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace Framework;

public class CameraRigController
{
    public bool IsEnabled { get; private set; }
    
    private IInputSystem InputSystem { get; }
    private IWindow Window { get; }
    private CameraRig CameraRig { get; }
    
    private bool IsFpsControlsEnabled { get; set; }
    
    public CameraRigController(CameraRig cameraRig, IWindow window, IInputSystem inputSystem)
    {
        CameraRig = cameraRig;
        InputSystem = inputSystem;
        Window = window;
    }

    public void Enable()
    {
        if (IsEnabled)
            return;
        
        var mouse = InputSystem.Mouse;
        mouse.Moved += OnMouseMoved;
        mouse.Scrolled += OnMouseWheelScrolled;
        mouse.ButtonPressed += OnMouseButtonPressed;
        mouse.ButtonReleased += OnMouseButtonReleased;
        
        if (mouse.IsButtonPressed(MouseButton.Right))
            EnableFpsControls();
        
        IsEnabled = true;
    }

    private void OnMouseButtonPressed(in MouseButtonStateChangedEvent evt)
    {
        if (evt.Button == MouseButton.Right)
        {
            EnableFpsControls();
        }
    }

    private void OnMouseButtonReleased(in MouseButtonStateChangedEvent evt)
    {
        if (evt.Button == MouseButton.Right)
        {
            DisableFpsControls();
        }
    }

    private void EnableFpsControls()
    {
        if (IsFpsControlsEnabled)
            return;

        Window.CursorMode = CursorMode.HiddenAndLocked;
        IsFpsControlsEnabled = true;
    }

    private void DisableFpsControls()
    {
        if (!IsFpsControlsEnabled)
            return;

        Window.CursorMode = CursorMode.Visible;
        IsFpsControlsEnabled = false;
    }

    public void Update(float dt)
    {
        if (!IsEnabled)
            return;
        
        if (!IsFpsControlsEnabled)
            return;
        
        var keyboard = InputSystem.Keyboard;
        var cameraRig = CameraRig;
        var distance = dt * 15f;

        if (keyboard.IsKeyPressed(KeyboardKey.W))
            cameraRig.MoveForward(distance);
        else if (keyboard.IsKeyPressed(KeyboardKey.S))
            cameraRig.MoveBackwards(distance);

        if (keyboard.IsKeyPressed(KeyboardKey.A))
            cameraRig.MoveLeft(distance);
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            cameraRig.MoveRight(distance);
    }

    public void Disable()
    {
        if (!IsEnabled)
            return;
        
        DisableFpsControls();
        var mouse = InputSystem.Mouse;
        mouse.Moved -= OnMouseMoved;
        mouse.Scrolled -= OnMouseWheelScrolled;
        IsEnabled = false;
    }

    private void OnMouseWheelScrolled(in MouseWheelScrolledEvent evt)
    {
        if (IsFpsControlsEnabled)
            return;
        
        var cameraRig = CameraRig;
        cameraRig.MoveForward(evt.DeltaY);
    }

    private void OnMouseMoved(in MouseMovedEvent evt)
    {
        var mouse = evt.Mouse;
        if (mouse.IsButtonPressed(MouseButton.Middle) && !IsFpsControlsEnabled)
        {
            var window = Window;
            var windowWidth = (float)window.Width;
            var deltaX = -evt.DeltaX / windowWidth * 100f;
            var deltaY = evt.DeltaY / windowWidth * 100f;
            CameraRig.Pan(deltaX, deltaY);
        }
        else if (mouse.IsButtonPressed(MouseButton.Left) && !IsFpsControlsEnabled)
        {
            var window = Window;
            var windowWidth = (float)window.Width;
            var deltaYaw = evt.DeltaX / windowWidth * -180f;
            var deltaPitch = evt.DeltaY / windowWidth * -180f;
            CameraRig.Orbit(deltaYaw, deltaPitch);
        }
        else if (mouse.IsButtonPressed(MouseButton.Right) && IsFpsControlsEnabled)
        {
            var window = Window;
            var windowWidth = (float)window.Width;
            var deltaYaw = evt.DeltaX / windowWidth * -180f;
            var deltaPitch = evt.DeltaY / windowWidth * -180f;
            CameraRig.Rotate(deltaYaw, deltaPitch);
        }
    }
}