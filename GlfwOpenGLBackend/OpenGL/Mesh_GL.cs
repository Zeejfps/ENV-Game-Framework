using System.Diagnostics;
using System.Numerics;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class Mesh_GL : IMesh
{
    public bool IsLoaded { get; }

    private readonly uint m_Vao;
    private readonly uint m_Vbo;
    private readonly uint m_Vio;
    private readonly uint m_UvBuffer;
    private readonly uint m_TangetBuffer;
    private readonly uint m_NormalsBuffer;

    private int m_TriangleCount;
    private uint m_InstancedVbo;

    public unsafe Mesh_GL(float[] vertices, float[] normals, float[] uvs, float[] tangents, int[] indices)
    {
        IsLoaded = true;
        m_Vao = glGenVertexArray();
        
        glBindVertexArray(m_Vao);
        
        m_Vbo = glGenBuffer();
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        fixed (float* v = &vertices[0])
            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
        glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
        glEnableVertexAttribArray(0);

        if (normals != null && normals.Length > 0)
        {
            m_NormalsBuffer = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, m_NormalsBuffer);
            fixed (float* v = &normals[0])
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * normals.Length, v, GL_STATIC_DRAW);
            glVertexAttribPointer(1, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
            glEnableVertexAttribArray(1);
        }

        if (uvs != null && uvs.Length > 0)
        {
            m_UvBuffer = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, m_UvBuffer);
            fixed (float* uv = &uvs[0])
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * uvs.Length, uv, GL_STATIC_DRAW);
            glVertexAttribPointer(2, 2, GL_FLOAT, false, 2 * sizeof(float), NULL);
            glEnableVertexAttribArray(2);  
        }

        if (tangents != null && tangents.Length > 0)
        {
            m_Vio = glGenBuffer();
            m_TangetBuffer = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, m_TangetBuffer);
            fixed (float* t = &tangents[0])
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * tangents.Length, t, GL_STATIC_DRAW);
            glVertexAttribPointer(3, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
            glEnableVertexAttribArray(3);
        }

        m_TriangleCount = indices.Length;
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, m_Vio);
        fixed (int* i = &indices[0])
            glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * indices.Length, i, GL_STATIC_DRAW);
    }

    public void Unload()
    {
        
    }

    public IMeshApi Use()
    {
        return Api.Use(this);
    }

    class Api : IMeshApi
    {
        private static Api? s_Instance;
        private static Api Instance => s_Instance ??= new Api();

        private Mesh_GL m_ActiveMesh;
        
        public static IMeshApi Use(Mesh_GL mesh)
        {
            Instance.m_ActiveMesh = mesh;
            glBindVertexArray(mesh.m_Vao);
            return Instance;
        }
        
        public void Render()
        {
            unsafe
            {
                glDrawElements(GL_TRIANGLES, Instance.m_ActiveMesh.m_TriangleCount, GL_UNSIGNED_INT, NULL);
            }
        }

        public void RenderInstanced(Matrix4x4[] transforms)
        {
            var perInstanceVbo = m_ActiveMesh.m_InstancedVbo;
            if (perInstanceVbo == 0)
            {
                perInstanceVbo = glGenBuffer();
                m_ActiveMesh.m_InstancedVbo = perInstanceVbo;
            }

            var data = new float[16 * transforms.Length];
            for (int i = 0, transformIndex = 0; i < data.Length; i += 16, transformIndex++)
            {
                var transform = transforms[transformIndex];
                data[i + 00] = transform.M11;
                data[i + 01] = transform.M12;
                data[i + 02] = transform.M13;
                data[i + 03] = transform.M14;
                
                data[i + 04] = transform.M21;
                data[i + 05] = transform.M22;
                data[i + 06] = transform.M23;
                data[i + 07] = transform.M24;
                
                data[i + 08] = transform.M31;
                data[i + 09] = transform.M32;
                data[i + 10] = transform.M33;
                data[i + 11] = transform.M34;
                
                data[i + 12] = transform.M41;
                data[i + 13] = transform.M42;
                data[i + 14] = transform.M43;
                data[i + 15] = transform.M44;
            }
            
            glEnableVertexAttribArray(5);
            glAssertNoError();
            
            glBindBuffer(GL_ARRAY_BUFFER, perInstanceVbo);
            glAssertNoError();

            unsafe
            {
                fixed (float* p = &data[0])
                    glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 16 * transforms.Length, p, GL_STATIC_DRAW);
                glAssertNoError();
            }
            
            glVertexAttribPointer(5, 16, GL_FLOAT, false, 16 * sizeof(float), IntPtr.Zero);
            glAssertNoError();

            glVertexAttribDivisor(5, 1);
            glAssertNoError();

            unsafe
            {
                glDrawElementsInstanced(GL_TRIANGLES, Instance.m_ActiveMesh.m_TriangleCount, GL_UNSIGNED_INT, NULL,
                    transforms.Length);
                glAssertNoError();
            }
        }

        public void Dispose()
        {
            //glBindVertexArray(0);
        }
    }

    static void GlCall(Action action)
    {
        action.Invoke();
        var error = glGetError();
        if (error != GL_NO_ERROR)
            throw new Exception($"GL ERROR OCCURED {error:X}");
    }

    [Conditional("DEBUG")]
    static void glAssertNoError()
    {
        var error = glGetError();
        if (error != GL_NO_ERROR)
        {
            var errorStr = $"Unknown Error {error:X}";
            switch (error)
            {
                case 0x0500:
                    errorStr = "GL_INVALID_ENUM";
                    break;
                case 0x0501:
                    errorStr = "GL_INVALID_VALUE";
                    break;
            }
            throw new Exception(errorStr);
        }
    }
}