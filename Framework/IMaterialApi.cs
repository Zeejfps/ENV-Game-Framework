using System.Numerics;

namespace Framework;

public interface IMaterialApi : IDisposable
{
    void SetFloat(string propertyName, float value);

    void SetVector3(string propertyName, float x, float y, float z);
    void SetVector3(string propertyName, Vector3 vector);

    void SetTexture2d(string propertyName, ITexture texture);
    
    void SetMatrix4x4(string propertyName, Matrix4x4 matrix);
    void SetMatrix4x4Array(string propertyName, Span<Matrix4x4> matrices);

    IBuffer GetBuffer(string name);
}

public interface IBuffer
{
    void Clear();
    void Put(Span<Matrix4x4> data);
    void Put(Matrix4x4 matrix);
    void Apply();
}