using System.Numerics;
using Framework;

namespace GlfwOpenGLBackend.AssetLoaders;

public class Material_GL : IMaterial
{
    public string Shader { get; }
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    
    public Material_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public void Use()
    {
        
    }
    
    public void Unload()
    {
        IsLoaded = false;
    }
    
    public void Apply(IShaderProgram shaderProgram)
    {
        throw new NotImplementedException();
    }

    public void SetVector3(string propertyName, float x, float y, float z)
    {
        throw new NotImplementedException();
    }

    public void SetVector3(string propertyName, Vector3 x)
    {
        throw new NotImplementedException();
    }

    public void SetTexture2d(string propertyName, ITexture texture)
    {
        throw new NotImplementedException();
    }

    public void SetMatrix4x4(string propertyName, Matrix4x4 matrix)
    {
        throw new NotImplementedException();
    }

    public void SetFloat(string propertyName, float x)
    {
        throw new NotImplementedException();
    }
}