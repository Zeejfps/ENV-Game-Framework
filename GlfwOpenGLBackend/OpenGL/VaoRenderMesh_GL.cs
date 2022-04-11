using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class VaoRenderMesh_GL : IRenderMesh
{
    private readonly uint m_Vao;
    private readonly uint m_Vbo;

    public unsafe VaoRenderMesh_GL(IMesh mesh)
    {
        // m_Vao = glGenVertexArray();
        // m_Vbo = glGenBuffer();
        //
        // var vertices = mesh.Vertices;
        //
        // glBindVertexArray(m_Vao);
        // glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        // fixed (float* v = &vertices[0])
        //     glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
        //
        // glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
        // glEnableVertexAttribArray(0);
    }

    public void Render()
    {
        glBindVertexArray(m_Vao);
        glDrawArrays(GL_TRIANGLES, 0, 3);
    }
}