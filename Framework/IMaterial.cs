using System.Numerics;

namespace Framework;

public interface IMaterial : IAsset
{
    void Use();

    void SetFloat(string propertyName, float value);

    void SetVector3(string propertyName, float x, float y, float z);
    void SetVector3(string propertyName, Vector3 vector);

    void SetTexture2d(string propertyName, ITexture texture);
    
    void SetMatrix4x4(string propertyName, Matrix4x4 matrix);
}