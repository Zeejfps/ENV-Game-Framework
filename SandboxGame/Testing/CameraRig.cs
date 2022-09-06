using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;

namespace Framework;

public class CameraRig
{
    public ICamera Camera { get; }
    private ITransform3D CameraTarget { get; }
    private IWindow Window { get; }

    private float CameraYawAngle { get; set; }
    private float CameraPitchAngle { get; set; }

    public CameraRig(IWindow window)
    {
        Window = window;
        var aspect = window.Width / (float)window.Height;
        Camera = new PerspectiveCamera(75f, aspect);
        CameraTarget = new Transform3D();
        Camera.Transform.WorldPosition = new Vector3(0, 5f, -25f);

        LookAtTarget();
    }
    
    public void Orbit(float deltaX, float deltaY)
    {
        var window = Window;
        deltaX /= window.Width;
        deltaY /= window.Width;
        
        var camera = Camera;
        var cameraTarget = CameraTarget;

        camera.Transform.RotateAround(cameraTarget.WorldPosition, Vector3.UnitY, -deltaX);
        camera.Transform.RotateAround(cameraTarget.WorldPosition, camera.Transform.Right, -deltaY);
        
        LookAtTarget();
    }

    public void Pan(float deltaX, float deltaY)
    {
        var camera = Camera;
        var cameraTarget = CameraTarget;
        var cameraTransform = camera.Transform;
        var window = Window;
        
        deltaX /= window.Width;
        deltaY /= window.Width;
        
        var movement = (camera.Transform.Right * -deltaX + camera.Transform.Up * deltaY);
        cameraTarget.WorldPosition += movement;
        cameraTransform.WorldPosition += movement;
    }

    public void Rotate(float deltaYaw, float deltaPitch)
    {
        var cameraTarget = CameraTarget;
        var cameraTransform = Camera.Transform;

        var distanceToTarget = (cameraTransform.WorldPosition - cameraTarget.WorldPosition).Length();
        
        CameraYawAngle += deltaYaw;
        CameraPitchAngle += deltaPitch;
        
        if (CameraPitchAngle < -89f)
            CameraPitchAngle = -89f;
        else if (CameraPitchAngle > 89f)
            CameraPitchAngle = 89f;

        var deg2Rad = MathF.PI / 180f;
        
        cameraTransform.WorldRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, CameraYawAngle * deg2Rad) * 
                                        Quaternion.CreateFromAxisAngle(Vector3.UnitX, CameraPitchAngle * deg2Rad);

        cameraTarget.WorldPosition = cameraTransform.WorldPosition + cameraTransform.Forward * distanceToTarget;
    }

    public void MoveForward(float distance)
    {
        var cameraTransform = Camera.Transform;
        Move(cameraTransform.Forward, distance);
    }

    public void MoveBackwards(float distance)
    {
        var cameraTransform = Camera.Transform;
        Move(-cameraTransform.Forward, distance);
    }

    public void MoveLeft(float distance)
    {
        var cameraTransform = Camera.Transform;
        Move(-cameraTransform.Right, distance);
    }

    public void MoveRight(float distance)
    {
        var cameraTransform = Camera.Transform;
        Move(cameraTransform.Right, distance);
    }

    private void Move(Vector3 direction, float distance)
    {
        var cameraTarget = CameraTarget;
        var cameraTransform = Camera.Transform;

        var movement = direction * distance;
        
        cameraTarget.WorldPosition += movement;
        cameraTransform.WorldPosition += movement;
    }

    private void LookAtTarget()
    {
        var cameraTarget = CameraTarget;
        var cameraTransform = Camera.Transform;
        var rad2deg = 180f / MathF.PI;

        cameraTransform.LookAt(cameraTarget.WorldPosition, Vector3.UnitY);
        cameraTransform.WorldRotation.ExtractYawPitchRoll(out var yaw, out var pitch, out _);
        
        CameraPitchAngle = pitch * rad2deg;
        CameraYawAngle = yaw * rad2deg;
    }
}