using static OpenGL.Gl;

namespace ENV.GLFW.NET;

public class IndexedRenderMesh_GL : IRenderMesh
{
    private readonly uint m_Vao;
    private readonly uint m_Vbo;
    private readonly uint m_Vio;
    private readonly uint m_UvBuffer;
    private readonly uint m_NormalsBuffer;

    private int m_TriangleCount;
    
    public unsafe IndexedRenderMesh_GL(IMesh mesh)
    {
        m_Vao = glGenVertexArray();
        m_Vbo = glGenBuffer();
        m_Vio = glGenBuffer();
        m_UvBuffer = glGenBuffer();
        m_NormalsBuffer = glGenBuffer();

        var vertices = mesh.Vertices;
        
        glBindVertexArray(m_Vao);
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        fixed (float* v = &vertices[0])
            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
        
        glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
        glEnableVertexAttribArray(0);

        var normals = mesh.Normals;

        glBindBuffer(GL_ARRAY_BUFFER, m_NormalsBuffer);
        fixed (float* v = &normals[0])
            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * normals.Length, v, GL_STATIC_DRAW);
        
        glVertexAttribPointer(1, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
        glEnableVertexAttribArray(1);

        var uvs = mesh.Uvs;
        
        glBindBuffer(GL_ARRAY_BUFFER, m_UvBuffer);
        fixed (float* uv = &uvs[0])
            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * uvs.Length, uv, GL_STATIC_DRAW);
        
        glVertexAttribPointer(2, 2, GL_FLOAT, false, 2 * sizeof(float), NULL);
        glEnableVertexAttribArray(2);  
        
        var indices = mesh.Triangles;
        m_TriangleCount = indices.Length;

        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, m_Vio);
        fixed (int* i = &indices[0])
            glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * indices.Length, i, GL_STATIC_DRAW);
    }
    
    public void Render()
    {
        unsafe
        {
            glBindVertexArray(m_Vao);
            glDrawElements(GL_TRIANGLES, m_TriangleCount, GL_UNSIGNED_INT, NULL);
        }
    }
}