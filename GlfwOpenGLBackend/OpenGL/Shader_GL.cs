using System.Diagnostics;
using System.Numerics;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class Shader_GL : IGpuShader
{
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    private readonly Dictionary<string, IBuffer> m_NameToBufferMap = new();
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    private int m_ActiveTextureId = 0;
    
    private Shader_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public bool EnableDepthTest { get; set; }
    public bool EnableBackfaceCulling { get; set; }
    public bool EnableBlending { get; set; }

    public IGpuShaderHandle Use()
    {
        return new Handle(this);
    }
    
    public void Dispose()
    {
        m_ProgramId = 0;
        IsLoaded = false;
    }

    public static Shader_GL LoadFromSource(string vertexShaderSource, string fragmentShaderSource)
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

        return new Shader_GL(program);
    }
    
    private static void CompileShader(uint shader, string source)
    {
        glShaderSource(shader, source);
        glCompileShader(shader);
        
        var error = glGetShaderInfoLog(shader);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling shader: {error}");
    }

    class Handle : IGpuShaderHandle
    {
        private static Handle? s_ActiveHandle;
        
        private Shader_GL Material { get; }
        private bool IsDisposed { get; set; }
        
        public Handle(Shader_GL material)
        {
            if (s_ActiveHandle != null)
                s_ActiveHandle.IsDisposed = true;

            s_ActiveHandle = this;
            Material = material;
            
            glUseProgram(material.m_ProgramId);
        
            if (material.EnableDepthTest)
                glEnable(GL_DEPTH_TEST);
            else
                glDisable(GL_DEPTH_TEST);

            if (material.EnableBackfaceCulling)
                glEnable(GL_CULL_FACE);
            else
                glDisable(GL_CULL_FACE);

            if (material.EnableBlending)
            {
                glEnable(GL_BLEND);
                glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_DST_ALPHA);
            }
            else
                glDisable(GL_BLEND);
        }

        public void SetFloat(string propertyName, float value)
        {
            Debug.Assert(!IsDisposed);
            var location = GetUniformLocation(propertyName);
            glUniform1f(location, value);
        }

        public void SetVector3(string propertyName, float x, float y, float z)
        {
            Debug.Assert(!IsDisposed);
            var location = GetUniformLocation(propertyName);
            glUniform3f(location, x, y, z);
        }

        public void SetVector3(string propertyName, Vector3 vector)
        {
            Debug.Assert(!IsDisposed);
            SetVector3(propertyName, vector.X, vector.Y, vector.Z);
        }

        public void SetTexture2d(string propertyName, IGpuTexture texture)
        {
            Debug.Assert(!IsDisposed);
            var location = GetUniformLocation(propertyName);
            var textureSlot = GetTextureSlot(propertyName);
            glUniform1i(location, textureSlot);
            glActiveTexture(GL_TEXTURE0 + textureSlot);
            texture.Use();
        }

        public void SetMatrix4x4(string propertyName, Matrix4x4 matrix)
        {
            Debug.Assert(!IsDisposed);
            var location = GetUniformLocation(propertyName);
            unsafe
            {
                var p = &matrix.M11;
                glUniformMatrix4fv(location, 1, false, p);
            }
        }

        public IBuffer GetBuffer(string name)
        {
            Debug.Assert(!IsDisposed);
            if (Material.m_NameToBufferMap.TryGetValue(name, out var buffer))
                return buffer;

            var index = glGetProgramResourceIndex(Material.m_ProgramId, GL_SHADER_STORAGE_BLOCK, name);
            glAssertNoError();
            
            buffer = new ShaderStorageBuffer_GL(index);
            Material.m_NameToBufferMap[name] = buffer;
            return buffer;
        }
        
        public void Dispose()
        {
            glUseProgram(0);
            IsDisposed = true;
        }
        
        private int GetUniformLocation(string uniformName)
        {
            Debug.Assert(Material != null);
            var propertyToIdMap = Material.m_PropertyToIdMap;
            if (!propertyToIdMap.TryGetValue(uniformName, out var location))
            {
                location = glGetUniformLocation(Material.m_ProgramId, uniformName);
                propertyToIdMap[uniformName] = location;
            }

            return location;
        }
        
        private int GetTextureSlot(string texture)
        {
            Debug.Assert(!IsDisposed);
            var textureToSlotMap = Material.m_TextureToSlotMap;
            if (textureToSlotMap.TryGetValue(texture, out var slot))
                return slot;

            slot = Material.m_ActiveTextureId;
            textureToSlotMap[texture] = slot;
            Material.m_ActiveTextureId++;
            return slot;
        }
    }
}