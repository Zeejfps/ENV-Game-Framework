using System.Numerics;

public sealed class Camera
{
    public Matrix4x4 ViewProjectionMatrix { get; private set; }

    private float _aspectRatio;
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            _aspectRatio = value;
            UpdateViewProjectionMatrix();
        }
    }

    private void UpdateViewProjectionMatrix()
    {
        var width = 200;
        var height = width / _aspectRatio;
        ViewProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, 0.1f, 100f);
    }

    public Camera(float aspectRatio)
    {
       _aspectRatio = aspectRatio;
       UpdateViewProjectionMatrix();
    }
}