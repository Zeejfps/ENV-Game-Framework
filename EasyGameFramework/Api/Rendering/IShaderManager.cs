using System.Numerics;
using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IShaderManager
{
    IHandle<IGpuShader> Load(string assetPath);
    void Bind(IHandle<IGpuShader>? handle);

    void SetFloat(string propertyName, float value);

    void SetVector3(string propertyName, float x, float y, float z);
    void SetVector2(string propertyName, Vector2 value);
    void SetVector2Array(string propertyName, ReadOnlySpan<Vector2> array);
    void SetVector3(string propertyName, Vector3 value);
    void SetVector3Array(string uniformName, ReadOnlySpan<Vector3> array);
    void SetTexture2d(string propertyName, IGpuTextureHandle value);
    void SetMatrix4x4(string propertyName, Matrix4x4 value);
    void SetMatrix4x4Array(string uniformName, ReadOnlySpan<Matrix4x4> array);

    IBufferHandle GetBuffer(string name);
}