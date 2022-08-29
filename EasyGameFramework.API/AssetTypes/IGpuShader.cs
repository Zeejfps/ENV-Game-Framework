using System.Numerics;

namespace EasyGameFramework.API.AssetTypes;

public interface IGpuShader : IGpuAsset
{
    void SetFloat(string propertyName, float value);

    void SetVector3(string propertyName, float x, float y, float z);
    void SetVector3(string propertyName, Vector3 vector);

    void SetTexture2d(string propertyName, IGpuTexture texture);

    void SetMatrix4x4(string propertyName, Matrix4x4 matrix);

    IBuffer GetBuffer(string name);
}