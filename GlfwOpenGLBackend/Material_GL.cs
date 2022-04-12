using System.Numerics;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.AssetLoaders;

public class Material_GL : IMaterial
{
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    private int m_ActiveTextureId = 0;
    
    private Material_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public bool IsDepthTestEnabled { get; set; }
    public bool IsBackfaceCullingEnabled { get; set; }

    public IMaterialApi Use()
    {
        return Api.Instance.Use(this);
    }
    
    public void Unload()
    {
        m_ProgramId = 0;
        IsLoaded = false;
    }

    public static Material_GL LoadFromSource(string vertexShaderSource, string fragmentShaderSource)
    {
        var vertexShader = glCreateShader(GL_VERTEX_SHADER);
        CompileShader(vertexShader, vertexShaderSource);

        var fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
        CompileShader(fragmentShader, fragmentShaderSource);
        
        var program = glCreateProgram();
        glAttachShader(program, vertexShader);
        glAttachShader(program, fragmentShader);

        glLinkProgram(program);

        var error = glGetProgramInfoLog(program);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling program:\n{error}");
        
        glDeleteShader(vertexShader);
        glDeleteShader(fragmentShader);

        return new Material_GL(program);
    }
    
    private static void CompileShader(uint shader, string source)
    {
        glShaderSource(shader, source);
        glCompileShader(shader);
        
        var error = glGetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling shader: {error}");
    }

    class Api : IMaterialApi
    {
        private static Api? m_Instance;
        public static Api Instance => m_Instance ??= new Api();

        private Material_GL m_ActiveMaterial;
        
        public IMaterialApi Use(Material_GL material)
        {
            m_ActiveMaterial = material;
            glUseProgram(material.m_ProgramId);
        
            if (material.IsDepthTestEnabled)
                glEnable(GL_DEPTH_TEST);
            else
                glDisable(GL_DEPTH_TEST);

            if (material.IsBackfaceCullingEnabled)
                glEnable(GL_CULL_FACE);
            else
                glDisable(GL_CULL_FACE);

            return this;
        }

        public void SetFloat(string propertyName, float value)
        {
            var location = GetUniformLocation(propertyName);
            glUniform1f(location, value);
        }

        public void SetVector3(string propertyName, float x, float y, float z)
        {
            var location = GetUniformLocation(propertyName);
            glUniform3f(location, x, y, z);
        }

        public void SetVector3(string propertyName, Vector3 vector)
        {
            SetVector3(propertyName, vector.X, vector.Y, vector.Z);
        }

        public void SetTexture2d(string propertyName, ITexture texture)
        {
            var location = GetUniformLocation(propertyName);
            var textureSlot = GetTextureSlot(propertyName);
            glUniform1i(location, textureSlot);
            glActiveTexture(GL_TEXTURE0 + textureSlot);
            texture.Use();
        }

        public void SetMatrix4x4(string propertyName, Matrix4x4 matrix)
        {
            var location = GetUniformLocation(propertyName);
            unsafe
            {
                var p = &matrix.M11;
                glUniformMatrix4fv(location, 1, false, p);
            }
        }
        
        public void Dispose()
        {
            m_ActiveMaterial = null;
            glUseProgram(0);
        }
        
        private int GetUniformLocation(string uniformName)
        {
            var propertyToIdMap = m_ActiveMaterial.m_PropertyToIdMap;
            if (!propertyToIdMap.TryGetValue(uniformName, out var location))
            {
                location = glGetUniformLocation(m_ActiveMaterial.m_ProgramId, uniformName);
                propertyToIdMap[uniformName] = location;
            }

            return location;
        }
        
        private int GetTextureSlot(string texture)
        {
            var textureToSlotMap = m_ActiveMaterial.m_TextureToSlotMap;
            if (textureToSlotMap.TryGetValue(texture, out var slot))
                return slot;

            slot = m_ActiveMaterial.m_ActiveTextureId;
            textureToSlotMap[texture] = slot;
            m_ActiveMaterial.m_ActiveTextureId++;
            return slot;
        }
    }
}