using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public interface IInstancedItem<TInstancedData> where TInstancedData : unmanaged
{
    event Action<IInstancedItem<TInstancedData>> BecameDirty;

    void UpdateInstanceData(ref TInstancedData instancedData);
}

public sealed unsafe class OpenGlTexturedQuadInstanceRenderer<TInstancedData> 
    where TInstancedData : unmanaged
{
    public int ItemCount => m_VertexAttribInstanceBuffer.ItemCount;
    
    private uint m_VertexArrayObject;
    private uint m_TexturedQuadBuffer;
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

        Span<uint> buffers = stackalloc uint[1];
        fixed (uint* ptr = &buffers[0])
            glGenBuffers(buffers.Length, ptr);
        AssertNoGlError();

        m_TexturedQuadBuffer = buffers[0];
        
        glBindVertexArray(m_VertexArrayObject);
        AssertNoGlError();
        
        SetupTexturedQuadBuffer();
        m_VertexAttribInstanceBuffer.Alloc();
    }
    
    private void SetupTexturedQuadBuffer()
    {
        glBindBuffer(GL_ARRAY_BUFFER, m_TexturedQuadBuffer);
        AssertNoGlError();

        var texturedQuad = new TexturedQuad();
        glBufferData(GL_ARRAY_BUFFER, SizeOf<TexturedQuad>(), &texturedQuad, GL_STATIC_DRAW);
        AssertNoGlError();
            
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
         Span<uint> buffers = stackalloc uint[]
         {
             m_TexturedQuadBuffer,
         };
         fixed (uint* ptr = &buffers[0])
             glDeleteBuffers(buffers.Length, ptr);
         AssertNoGlError();
         m_TexturedQuadBuffer = 0;
         
         m_VertexAttribInstanceBuffer.Free();
     }
}