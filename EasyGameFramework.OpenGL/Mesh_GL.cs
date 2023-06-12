using EasyGameFramework.Api.AssetTypes;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class Mesh_GL : IGpuMesh
{
    private readonly uint m_NormalsBuffer;
    private readonly uint m_TangetBuffer;
    private readonly uint m_UvBuffer;

    private readonly uint m_Vbo;
    private readonly uint m_Vio;
    private uint m_InstancedVbo;

    private readonly int m_TriangleCount;

    public unsafe Mesh_GL(float[] vertices, float[] normals, float[] uvs, float[] tangents, int[] indices)
    {
        IsLoaded = true;
        VaoId = glGenVertexArray();

        glBindVertexArray(VaoId);

        m_Vbo = glGenBuffer();
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        fixed (float* v = &vertices[0])
        {
            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
        }

        glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
        glEnableVertexAttribArray(0);

        if (normals != null && normals.Length > 0)
        {
            m_NormalsBuffer = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, m_NormalsBuffer);
            fixed (float* v = &normals[0])
            {
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * normals.Length, v, GL_STATIC_DRAW);
            }

            glVertexAttribPointer(1, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
            glEnableVertexAttribArray(1);
        }

        if (uvs != null && uvs.Length > 0)
        {
            m_UvBuffer = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, m_UvBuffer);
            fixed (float* uv = &uvs[0])
            {
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * uvs.Length, uv, GL_STATIC_DRAW);
            }

            glVertexAttribPointer(2, 2, GL_FLOAT, false, 2 * sizeof(float), NULL);
            glEnableVertexAttribArray(2);
        }

        if (tangents != null && tangents.Length > 0)
        {
            m_TangetBuffer = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, m_TangetBuffer);
            fixed (float* t = &tangents[0])
            {
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * tangents.Length, t, GL_STATIC_DRAW);
            }

            glVertexAttribPointer(3, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
            glEnableVertexAttribArray(3);
        }

        m_TriangleCount = indices.Length;
        
        m_Vio = glGenBuffer();
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, m_Vio);
        glAssertNoError();

        fixed (int* i = &indices[0])
        {
            glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * indices.Length, i, GL_STATIC_DRAW);
        }

        glAssertNoError();
    }

    public bool IsLoaded { get; }
    public uint VaoId { get; }

    public void Dispose()
    {
    }

    public void Render()
    {
        unsafe
        {
            glDrawElements(GL_TRIANGLES, m_TriangleCount, GL_UNSIGNED_INT, NULL);
        }
    }

    public void RenderInstanced(int instanceCount)
    {
        unsafe
        {
            glDrawElementsInstanced(GL_TRIANGLES, m_TriangleCount, GL_UNSIGNED_INT, NULL, instanceCount);
            glAssertNoError();
        }
    }
}