using System.Diagnostics;
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
    private IClock Clock { get; }
    
    private bool IsFpsControlsEnabled { get; set; }
    
    public CameraRigController(
        CameraRig cameraRig, 
        IWindow window, 
        IInputSystem inputSystem,
        IClock clock)
    {
        CameraRig = cameraRig;
        InputSystem = inputSystem;
        Window = window;
        Clock = clock;
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

        Clock.Ticked += Update;
        IsEnabled = true;
    }

    private void OnMouseButtonPressed(in MouseButtonStateChangedEvent evt)
    {
        var button = evt.Button;
        if (button == MouseButton.Right)
        {
            EnableFpsControls();
        }
        else if (button == MouseButton.Left)
        {
            Window.CursorMode = CursorMode.HiddenAndLocked;
        }
    }

    private void OnMouseButtonReleased(in MouseButtonStateChangedEvent evt)
    {
        var button = evt.Button;
        if (button == MouseButton.Right)
        {
            DisableFpsControls();
        }
        else if (button == MouseButton.Left)
        {
            Window.CursorMode = CursorMode.Visible;
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

    private void Update()
    {
        Debug.Assert(IsEnabled);
        
        if (!IsFpsControlsEnabled)
            return;
        
        var keyboard = InputSystem.Keyboard;
        var cameraRig = CameraRig;
        var distance = Clock.DeltaTime * 15f;

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
        
        Clock.Ticked -= Update;

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
            var windowWidth = (float)window.ViewportWidth;
            var deltaX = -evt.DeltaX / windowWidth * 100f;
            var deltaY = evt.DeltaY / windowWidth * 100f;
            CameraRig.Pan(deltaX, deltaY);
        }
        else if (mouse.IsButtonPressed(MouseButton.Left) && !IsFpsControlsEnabled)
        {
            var window = Window;
            var windowWidth = (float)window.ViewportWidth;
            var deltaYaw = evt.DeltaX / windowWidth * -180f;
            var deltaPitch = evt.DeltaY / windowWidth * -180f;
            CameraRig.Orbit(deltaYaw, deltaPitch);
        }
        else if (mouse.IsButtonPressed(MouseButton.Right) && IsFpsControlsEnabled)
        {
            var window = Window;
            var windowWidth = (float)window.ViewportWidth;
            var deltaYaw = evt.DeltaX / windowWidth * -180f;
            var deltaPitch = evt.DeltaY / windowWidth * -180f;
            CameraRig.Rotate(deltaYaw, deltaPitch);
        }
    }
}