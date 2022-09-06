using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace Framework;

public class CameraController
{
    public bool IsEnabled { get; private set; }
    
    private ICamera Camera { get; }
    private ITransform3D CameraTarget { get; }
    private IInputSystem InputSystem { get; }
    private IWindow Window { get; }
    
    public CameraController(ICamera camera, IWindow window, IInputSystem inputSystem)
    {
        Camera = camera;
        CameraTarget = new Transform3D();
        InputSystem = inputSystem;
        Window = window;
    }

    public void Enable()
    {
        if (IsEnabled)
            return;
        
        var mouse = InputSystem.Mouse;
        var camera = Camera;
        var cameraTarget = CameraTarget;

        camera.Transform.LookAt(cameraTarget.WorldPosition, Vector3.UnitY);

        mouse.Moved += OnMouseMoved;
        mouse.Scrolled += OnMouseWheelScrolled;
        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled)
            return;
        
        var mouse = InputSystem.Mouse;
        mouse.Moved -= OnMouseMoved;
        mouse.Scrolled -= OnMouseWheelScrolled;
        IsEnabled = false;
    }

    public void Update(float dt)
    {
        if (!IsEnabled)
            return;
        
        var camera = Camera;
        var speed = dt * 15f;
        var keyboard = InputSystem.Keyboard;
        var cameraTarget = CameraTarget;
        var cameraTransform = camera.Transform;
        
        if (keyboard.IsKeyPressed(KeyboardKey.W))
            cameraTransform.WorldPosition += cameraTransform.Forward * speed;
        else if (keyboard.IsKeyPressed(KeyboardKey.S))
            cameraTransform.WorldPosition -= cameraTransform.Forward * speed;
        
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            cameraTransform.WorldPosition -= cameraTransform.Right * speed;
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            cameraTransform.WorldPosition += cameraTransform.Right * speed;
        
        cameraTransform.LookAt(cameraTarget.WorldPosition, Vector3.UnitY);
    }
    
    private void OnMouseWheelScrolled(in MouseWheelScrolledEvent evt)
    {
        var camera = Camera;
        camera.Transform.WorldPosition += camera.Transform.Forward * evt.DeltaY;
    }

    private void OnMouseMoved(in MouseMovedEvent evt)
    {
        var mouse = evt.Mouse;
        if (mouse.IsButtonPressed(MouseButton.Left))
            RotateCamera(evt.DeltaX, evt.DeltaY);
        else if (mouse.IsButtonPressed(MouseButton.Middle))
            PanCamera(evt.DeltaX, evt.DeltaY);
    }

    private void RotateCamera(float deltaX, float deltaY)
    {
        deltaX /= Window.Width;
        deltaY /= Window.Width;
        
        var camera = Camera;
        var cameraTarget = CameraTarget;
        camera.Transform.RotateAround(cameraTarget.WorldPosition, Vector3.UnitY, -deltaX);
        camera.Transform.RotateAround(cameraTarget.WorldPosition, camera.Transform.Right, -deltaY);
    }

    private void PanCamera(float deltaX, float deltaY)
    {
        var camera = Camera;
        var cameraTarget = CameraTarget;

        var movement = (camera.Transform.Right * -deltaX + camera.Transform.Up * deltaY) / Window.Width;
        camera.Transform.WorldPosition += movement;
        cameraTarget.WorldPosition += movement;
    }

}