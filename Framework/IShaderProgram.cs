using System.Numerics;

namespace Framework;

public interface IShaderProgram
{
    void SetVector3f(string name, float x, float y, float z);
    void SetMatrix4x4f(string name, Matrix4x4 matrix);

    void SetFloat(string name, float x);
    void SetTexture2d(string name, ITexture value);
}