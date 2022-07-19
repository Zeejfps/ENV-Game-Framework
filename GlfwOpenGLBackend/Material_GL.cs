using System.Diagnostics;
using System.Numerics;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class Material_GL : IMaterial
{
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    private readonly Dictionary<string, IBuffer> m_NameToBufferMap = new();
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    private int m_ActiveTextureId = 0;
    
    private Material_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public bool EnableDepthTest { get; set; }
    public bool EnableBackfaceCulling { get; set; }
    public bool EnableBlending { get; set; }

    public IMaterialHandle Use()
    {
        return Handle.Use(this);
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

    class Handle : IMaterialHandle
    {
        private static Handle? s_Instance;
        private static Handle Instance => s_Instance ??= new Handle();
        private Material_GL? ActiveMaterial { get; set; }
        
        private readonly Stack<Material_GL> m_MaterialStack = new();

        public static IMaterialHandle Use(Material_GL material)
        {
            if (Instance.ActiveMaterial != null)
                Instance.m_MaterialStack.Push(Instance.ActiveMaterial);
            
            Instance.ActiveMaterial = material;
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

            return Instance;
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

        public IBuffer GetBuffer(string name)
        {
            Debug.Assert(ActiveMaterial != null);
            if (ActiveMaterial.m_NameToBufferMap.TryGetValue(name, out var buffer))
                return buffer;

            var index = glGetProgramResourceIndex(ActiveMaterial.m_ProgramId, GL_SHADER_STORAGE_BLOCK, name);
            glAssertNoError();
            
            buffer = new ShaderStorageBuffer_GL(index);
            ActiveMaterial.m_NameToBufferMap[name] = buffer;
            return buffer;
        }
        
        public void Dispose()
        {
            if (m_MaterialStack.TryPop(out var material))
                Use(material);
            else
                glUseProgram(0);
        }
        
        private int GetUniformLocation(string uniformName)
        {
            Debug.Assert(ActiveMaterial != null);
            var propertyToIdMap = ActiveMaterial.m_PropertyToIdMap;
            if (!propertyToIdMap.TryGetValue(uniformName, out var location))
            {
                location = glGetUniformLocation(ActiveMaterial.m_ProgramId, uniformName);
                propertyToIdMap[uniformName] = location;
            }

            return location;
        }
        
        private int GetTextureSlot(string texture)
        {
            Debug.Assert(ActiveMaterial != null);
            var textureToSlotMap = ActiveMaterial.m_TextureToSlotMap;
            if (textureToSlotMap.TryGetValue(texture, out var slot))
                return slot;

            slot = ActiveMaterial.m_ActiveTextureId;
            textureToSlotMap[texture] = slot;
            ActiveMaterial.m_ActiveTextureId++;
            return slot;
        }
    }
}