using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;

namespace Framework;

public class CameraRig
{
    private const float Deg2Rad = MathF.PI / 180f;
    private const float Rad2Deg = 180f / MathF.PI;

    public ICamera Camera { get; }
    private Vector3 CameraTarget { get; set; }

    private float CameraYawAngle { get; set; }
    private float CameraPitchAngle { get; set; }

    public CameraRig(ICamera camera)
    {
        Camera = camera;
        CameraTarget = new Vector3();

        LookAtTarget();
    }
    
    public void Orbit(float deltaYaw, float deltaPitch)
    {
        var camera = Camera;
        var cameraTarget = CameraTarget;
        
        camera.Transform.RotateAround(cameraTarget, Vector3.UnitY, deltaYaw * Deg2Rad);
        camera.Transform.RotateAround(cameraTarget, camera.Transform.Right, deltaPitch * Deg2Rad);
        
        LookAtTarget();
    }

    public void Pan(float leftDelta, float upDelta)
    {
        var camera = Camera;
        var movement = camera.Transform.Right * leftDelta + camera.Transform.Up * upDelta;
        Move(movement);
    }

    public void Rotate(float deltaYaw, float deltaPitch)
    {
        var cameraTarget = CameraTarget;
        var cameraTransform = Camera.Transform;

        var distanceToTarget = (cameraTransform.WorldPosition - cameraTarget).Length();
        
        CameraYawAngle += deltaYaw;
        CameraPitchAngle += deltaPitch;
        
        if (CameraPitchAngle < -89f)
            CameraPitchAngle = -89f;
        else if (CameraPitchAngle > 89f)
            CameraPitchAngle = 89f;
        
        cameraTransform.WorldRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, CameraYawAngle * Deg2Rad) * 
                                        Quaternion.CreateFromAxisAngle(Vector3.UnitX, CameraPitchAngle * Deg2Rad);

        cameraTarget = cameraTransform.WorldPosition + cameraTransform.Forward * distanceToTarget;

        CameraTarget = cameraTarget;
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
        var movement = direction * distance;
        Move(movement);
    }

    private void Move(Vector3 movement)
    {
        var cameraTarget = CameraTarget;
        var cameraTransform = Camera.Transform;
        cameraTarget += movement;
        cameraTransform.WorldPosition += movement;

        CameraTarget = cameraTarget;
    }

    private void LookAtTarget()
    {
        var cameraTarget = CameraTarget;
        var cameraTransform = Camera.Transform;

        cameraTransform.LookAt(cameraTarget, Vector3.UnitY);
        cameraTransform.WorldRotation.ExtractYawPitchRoll(out var yaw, out var pitch, out _);
        
        CameraPitchAngle = pitch * Rad2Deg;
        CameraYawAngle = yaw * Rad2Deg;
    }
}