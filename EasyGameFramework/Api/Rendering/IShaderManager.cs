using System.Numerics;
using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IShaderManager
{
    IHandle<IGpuShader> Load(string assetPath);
    void Bind(IHandle<IGpuShader>? handle);

    void SetFloat(string propertyName, float value);

    void SetVector3(string propertyName, float x, float y, float z);
    void SetVector3(string propertyName, Vector3 vector);

    void SetTexture2d(string propertyName, IHandle<IGpuTexture> value);

    void SetMatrix4x4(string propertyName, Matrix4x4 value);

    IBuffer GetBuffer(string name);
}