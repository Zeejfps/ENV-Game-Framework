using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public sealed unsafe class OpenGlTexturedQuadInstanceRenderer<TInstancedData> 
    where TInstancedData : unmanaged
{
    public int ItemCount => m_VertexAttribInstanceBuffer.ItemCount;
    
    private uint m_VertexArrayObject;
    private readonly ArrayBuffer<TexturedQuad> m_TexturedQuadBuffer = new();
    private readonly VertexAttribInstanceBuffer<TInstancedData> m_VertexAttribInstanceBuffer;

    public OpenGlTexturedQuadInstanceRenderer(uint maxInstanceCount)
    {
        m_VertexAttribInstanceBuffer = new VertexAttribInstanceBuffer<TInstancedData>(2, maxInstanceCount);
    }
    
    public void Add(IInstancedItem<TInstancedData> item)
    {
        m_VertexAttribInstanceBuffer.Add(item);
    }

    public void Remove(IInstancedItem<TInstancedData> item)
    {
        m_VertexAttribInstanceBuffer.Remove(item);
    }
    
    public void Render()
    {
        m_VertexAttribInstanceBuffer.Update();
        
        glBindVertexArray(m_VertexArrayObject);
        AssertNoGlError();
        
        glDrawArraysInstanced(GL_TRIANGLES, 0, 6, ItemCount);
        AssertNoGlError();
    }
    
    public void Load()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        m_VertexArrayObject = vao;
        
        glBindVertexArray(m_VertexArrayObject);
        AssertNoGlError();
        
        SetupTexturedQuadBuffer();
        m_VertexAttribInstanceBuffer.Alloc();
    }
    
    private void SetupTexturedQuadBuffer()
    {
        m_TexturedQuadBuffer.AllocAndWrite(new TexturedQuad(), MutableBufferUsageHints.StaticDraw);

        uint positionAttribIndex = 0;
        glVertexAttribPointer(
            positionAttribIndex, 
            2, 
            GL_FLOAT, 
            false, 
            sizeof(TexturedQuad.Vertex), 
            Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.Position))
        );
        glEnableVertexAttribArray(positionAttribIndex);

        uint normalAttribIndex = 1;
        glVertexAttribPointer(
            normalAttribIndex, 
            2, 
            GL_FLOAT,
            false, 
            sizeof(TexturedQuad.Vertex),
            Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords))
        );
        glEnableVertexAttribArray(normalAttribIndex);
    }
    
     public void Unload()
     {
         glBindVertexArray(0);
         fixed(uint* ptr = &m_VertexArrayObject)
             glDeleteVertexArrays(1, ptr);
         AssertNoGlError();
         m_VertexArrayObject = 0;
            
         glBindBuffer(GL_ARRAY_BUFFER, 0);
         
         m_TexturedQuadBuffer.Free();
         m_VertexAttribInstanceBuffer.Free();
     }
}