namespace Framework;

public interface IShaderProgram
{
    void SetVector3f(string name, float x, float y, float z);
    void SetMatrix4x4f(string name, float[] matrix);

    void SetFloat(string name, float x);
    void SetTexture2d(string name, ITexture value);
}