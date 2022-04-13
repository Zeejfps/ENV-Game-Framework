using System.Diagnostics;
using System.Numerics;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class Material_GL : IMaterial
{
    private readonly Dictionary<string, int> m_PropertyToIdMap = new();
    private readonly Dictionary<string, int> m_TextureToSlotMap = new();
    public bool IsLoaded { get; private set; }

    private uint m_ProgramId;
    private int m_ActiveTextureId = 0;
    private uint ssbo;

    private float[] m_MatriciesData = new float[0];

    private Material_GL(uint programId)
    {
        m_ProgramId = programId;
        IsLoaded = true;
    }

    public bool UseDepthTest { get; set; }
    public bool UseBackfaceCulling { get; set; }

    public IMaterialApi Use()
    {
        return Api.Use(this);
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
        private static Api? s_Instance;
        private static Api Instance => s_Instance ??= new Api();
        private Material_GL? ActiveMaterial { get; set; }
        
        private readonly Stack<Material_GL> m_MaterialStack = new();

        public static IMaterialApi Use(Material_GL material)
        {
            if (Instance.ActiveMaterial != null)
                Instance.m_MaterialStack.Push(Instance.ActiveMaterial);
            
            Instance.ActiveMaterial = material;
            glUseProgram(material.m_ProgramId);
        
            if (material.UseDepthTest)
                glEnable(GL_DEPTH_TEST);
            else
                glDisable(GL_DEPTH_TEST);

            if (material.UseBackfaceCulling)
                glEnable(GL_CULL_FACE);
            else
                glDisable(GL_CULL_FACE);

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
        
        public void SetMatrix4x4Array(string propertyName, Matrix4x4[] matrices)
        {
            Debug.Assert(ActiveMaterial != null);
            
            if (ActiveMaterial.ssbo == 0)
                ActiveMaterial.ssbo = glGenBuffer();

            glBindBuffer(GL_SHADER_STORAGE_BUFFER, ActiveMaterial.ssbo);
            glAssertNoError();

            var dataLength = matrices.Length * 16;
            var needsResizing = false;
            if (ActiveMaterial.m_MatriciesData.Length < dataLength)
            {
                Array.Resize(ref ActiveMaterial.m_MatriciesData, dataLength);
                needsResizing = true;
            }

            var data = ActiveMaterial.m_MatriciesData;
            for (int i = 0, j = 0; i < dataLength; i += 16, j++)
            {
                var matrix = matrices[j];
                
                data[i + 00] = matrix.M11;
                data[i + 01] = matrix.M12;
                data[i + 02] = matrix.M13;
                data[i + 03] = matrix.M14;
                
                data[i + 04] = matrix.M21;
                data[i + 05] = matrix.M22;
                data[i + 06] = matrix.M23;
                data[i + 07] = matrix.M24;
                
                data[i + 08] = matrix.M31;
                data[i + 09] = matrix.M32;
                data[i + 10] = matrix.M33;
                data[i + 11] = matrix.M34;
                
                data[i + 12] = matrix.M41;
                data[i + 13] = matrix.M42;
                data[i + 14] = matrix.M43;
                data[i + 15] = matrix.M44;
            }


            if (needsResizing)
            {
                unsafe
                {
                    fixed (float* p = &data[0])
                        glBufferData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * dataLength, p, GL_DYNAMIC_COPY);
                    glAssertNoError();
                }
            }
            else
            {
                unsafe
                {
                    fixed (float* p = &data[0])
                        glBufferSubData(GL_SHADER_STORAGE_BUFFER, 0, sizeof(float) * dataLength, p);
                    glAssertNoError();
                }
            }

            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 0, ActiveMaterial.ssbo);
            glAssertNoError();

            glBindBuffer(GL_SHADER_STORAGE_BUFFER, 0);
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