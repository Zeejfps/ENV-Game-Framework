using System.Numerics;

public sealed class Camera
{
    public float MinZoomFactor = 0.25f;
    public float MaxZoomFactor = 3.0f;

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

    private float _zoomFactor;
    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            if (value < MinZoomFactor)
                value = MinZoomFactor;
            else if (value > MaxZoomFactor)
                value = MaxZoomFactor;
            _zoomFactor = value;
            UpdateViewProjectionMatrix();
        }
    }

    private Vector2 _position;

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            UpdateViewProjectionMatrix();
        }
    }

    private void UpdateViewProjectionMatrix()
    {
        var width = 200 * _zoomFactor;
        var height = width / _aspectRatio;
        var projMatrix = Matrix4x4.CreateOrthographic(width, height, 0.1f, 100f);

        var position = new Vector3(-_position.X, -_position.Y, 0.0f);
        var viewMatrix = Matrix4x4.CreateTranslation(position);

        ViewProjectionMatrix = viewMatrix * projMatrix;
    }

    public Camera(float aspectRatio)
    {
        _zoomFactor = 1f;
        _aspectRatio = aspectRatio;
        UpdateViewProjectionMatrix();
    }
}