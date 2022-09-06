using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace Framework;

public class CameraController
{
    private ICamera Camera { get; }
    private ITransform3D CameraTarget { get; }
    
    private IInputSystem InputSystem { get; }
    private IWindow Window { get; }
    
    public CameraController(ICamera camera, ITransform3D cameraTarget, IWindow window, IInputSystem inputSystem)
    {
        Camera = camera;
        CameraTarget = cameraTarget;
        InputSystem = inputSystem;
        Window = window;
    }

    public void Bind()
    {
        InputSystem.Mouse.Moved += OnMouseMoved;
    }

    public void Update(float dt)
    {
        var camera = Camera;
        var speed = dt * 15f;
        var keyboard = InputSystem.Keyboard;
        var cameraTarget = CameraTarget;
        
        if (keyboard.IsKeyPressed(KeyboardKey.W))
            camera.Transform.WorldPosition += camera.Transform.Forward * speed;
        else if (keyboard.IsKeyPressed(KeyboardKey.S))
            camera.Transform.WorldPosition -= camera.Transform.Forward * speed;
        
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            camera.Transform.WorldPosition -= camera.Transform.Right * speed;
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            camera.Transform.WorldPosition += camera.Transform.Right * speed;
        
        camera.Transform.LookAt(cameraTarget.WorldPosition, Vector3.UnitY);
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