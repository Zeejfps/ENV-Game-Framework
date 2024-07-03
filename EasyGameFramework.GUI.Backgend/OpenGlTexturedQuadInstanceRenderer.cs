using System.Diagnostics;
using System.Reflection;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public interface IInstancedItem<TInstancedData> where TInstancedData : unmanaged
{
    event Action<IInstancedItem<TInstancedData>> BecameDirty;

    void Update(ref TInstancedData instancedData);
}

public sealed unsafe class OpenGlTexturedQuadInstanceRenderer<TInstancedData> 
    where TInstancedData : unmanaged
{
    public int ItemCount => m_ItemCount;
    
    private readonly HashSet<IInstancedItem<TInstancedData>> m_ItemsToRegister = new();
    private readonly HashSet<IInstancedItem<TInstancedData>> m_ItemsToUnregister = new();
    private readonly HashSet<IInstancedItem<TInstancedData>> m_DirtyItems = new();
    private readonly Dictionary<IInstancedItem<TInstancedData>, int> m_ItemToIndexTable = new();
    private readonly Dictionary<int, IInstancedItem<TInstancedData>> m_IndexToItemTable = new();
    private readonly SortedSet<int> m_IdsToFill = new();

    private readonly uint m_MaxInstanceCount;
    
    private uint m_VertexArrayObject;
    private uint m_TexturedQuadBuffer;
    private uint m_InstancedDataBuffer;
    
    private int m_ItemCount;

    public OpenGlTexturedQuadInstanceRenderer(uint maxInstanceCount)
    {
        m_MaxInstanceCount = maxInstanceCount;
    }
    
    public void Add(IInstancedItem<TInstancedData> item)
    {
        m_ItemsToRegister.Add(item);
        m_ItemsToUnregister.Remove(item);
    }

    public void Remove(IInstancedItem<TInstancedData> item)
    {
        m_ItemsToUnregister.Add(item);
        m_ItemsToRegister.Remove(item);
    }

    public void Update()
    {
        //Console.WriteLine($"Unregistering {m_ItemsToUnregister.Count} panels");
        foreach (var item in m_ItemsToUnregister)
        {
            item.BecameDirty -= Item_OnBecameDirty;
            var id = m_ItemToIndexTable[item];
            m_IdsToFill.Add(id);
            m_IndexToItemTable.Remove(id);
            m_ItemToIndexTable.Remove(item);
            m_DirtyItems.Remove(item);
        }
        m_ItemsToUnregister.Clear();
            
        //Console.WriteLine($"Registering {m_ItemsToRegister.Count} items");
        foreach (var item in m_ItemsToRegister)
        {
            item.BecameDirty += Item_OnBecameDirty;
            int id;
            if (m_IdsToFill.Count > 0)
            {
                id = m_IdsToFill.Min;
                //Console.WriteLine($"Reusing an id that needs to be filled. Id: {id}");
                m_IdsToFill.Remove(id);
            }
            else
            {
                id = m_ItemCount;
                //Console.WriteLine($"Assigned a new id. Id: {id}");
                m_ItemCount++;
            }

            m_ItemToIndexTable[item] = id;
            m_IndexToItemTable[id] = item;
                
            m_DirtyItems.Add(item);
        }
        m_ItemsToRegister.Clear();
            
        //Console.WriteLine($"Back filling {m_IdsToFill.Count} ids");
        foreach (var idToFill in m_IdsToFill.Reverse())
        {
            var lastPanelId = m_ItemCount - 1;
            if (idToFill != lastPanelId)
            {
                Console.WriteLine($"Moving last panel into an id we need to fill. Id: {idToFill}");
                var lastPanel = m_IndexToItemTable[lastPanelId];

                m_IndexToItemTable.Remove(lastPanelId);
                m_IndexToItemTable[idToFill] = lastPanel;
                m_ItemToIndexTable[lastPanel] = idToFill;

                m_DirtyItems.Add(lastPanel);
            }
                
            m_ItemCount--;
        }
        m_IdsToFill.Clear();
        
        var maxIndex = 0;
        foreach (var dirtyItem in m_DirtyItems)
        {
            var index = m_ItemToIndexTable[dirtyItem];
            if (index > maxIndex)
                maxIndex = index;
        }
        
        //Console.WriteLine($"Max dirty panel index {maxIndex}");

        var bufferRangeSize = maxIndex + 1;

        if (m_DirtyItems.Count > 0)
        {
            //Console.WriteLine($"Have dirty items: {m_DirtyItems.Count}");

            glBindBuffer(GL_ARRAY_BUFFER, m_InstancedDataBuffer);
            AssertNoGlError();
            var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, SizeOf<TInstancedData>(bufferRangeSize), GL_MAP_WRITE_BIT);
            AssertNoGlError();
            var buffer = new Span<TInstancedData>(bufferPtr, bufferRangeSize);

            var dstIndex = 0;
            foreach (var srcItem in m_DirtyItems)
            {
                var dirtyItemIndex = m_ItemToIndexTable[srcItem];
   
                if (dirtyItemIndex > dstIndex)
                {
                    //Console.WriteLine($"Swaping {panelId} with {dstIndex}");
                    var srcIndex = dirtyItemIndex;

                    var dstPanel = m_IndexToItemTable[dstIndex];
            
                    var dstPanelData = buffer[dstIndex];
                    buffer[srcIndex] = dstPanelData;
            
                    m_IndexToItemTable[srcIndex] = dstPanel;
                    m_ItemToIndexTable[dstPanel] = srcIndex;

                    m_IndexToItemTable[dstIndex] = srcItem;
                    m_ItemToIndexTable[srcItem] = dstIndex;
                }

                srcItem.Update(ref buffer[dstIndex]);
                //Console.WriteLine(buffer[dstIndex]);
                dstIndex++;
            }
            //Console.WriteLine($"Updated Dirty Items: {m_DirtyItems.Count}");
            m_DirtyItems.Clear();
            glUnmapBuffer(GL_ARRAY_BUFFER);
        }
            
        //Console.WriteLine($"Dirty Count: {m_DirtyCount}, Panel Count: {m_PanelCount}");
    }

    public void Render()
    {
        glBindVertexArray(m_VertexArrayObject);
        AssertNoGlError();
        
        glDrawArraysInstanced(GL_TRIANGLES, 0, 6, m_ItemCount);
        AssertNoGlError();
    }
    
    private void Item_OnBecameDirty(IInstancedItem<TInstancedData> item)
    {
        m_DirtyItems.Add(item);
    }

    public void Load()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        m_VertexArrayObject = vao;

        Span<uint> buffers = stackalloc uint[2];
        fixed (uint* ptr = &buffers[0])
            glGenBuffers(buffers.Length, ptr);
        AssertNoGlError();

        m_TexturedQuadBuffer = buffers[0];
        m_InstancedDataBuffer = buffers[1];
        
        glBindVertexArray(m_VertexArrayObject);
        AssertNoGlError();
        
        SetupTexturedQuadBuffer();
        SetupInstancedDataBuffer();
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
    
     private void SetupInstancedDataBuffer()
    {
        glBindBuffer(GL_ARRAY_BUFFER, m_InstancedDataBuffer);
        glBufferData(GL_ARRAY_BUFFER, SizeOf<TInstancedData>(m_MaxInstanceCount), (void*)0, GL_DYNAMIC_DRAW);

        var instancedDataType = typeof(TInstancedData);
        var fields = instancedDataType.GetFields()
            .Where(fieldInfo => fieldInfo.GetCustomAttribute<InstancedAttrib>() != null);

        uint attribIndex = 2;
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<InstancedAttrib>();
            Debug.Assert(attribute != null);
            glVertexAttribPointer(
                attribIndex, 
                attribute.ComponentCount, 
                attribute.ComponentType, 
                false, 
                sizeof(TInstancedData), 
                Offset<TInstancedData>(field.Name)
            );
            //Console.WriteLine($"Attribute: {attribIndex}, Offset: " + (int)Offset<TInstancedData>(field.Name));
            glEnableVertexAttribArray(attribIndex);
            glVertexAttribDivisor(attribIndex, 1);
            attribIndex++;
        }
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
             m_InstancedDataBuffer
         };
         fixed (uint* ptr = &buffers[0])
             glDeleteBuffers(buffers.Length, ptr);
         AssertNoGlError();
         m_TexturedQuadBuffer = 0;
         m_InstancedDataBuffer = 0;

         m_ItemCount = 0;
         m_ItemsToRegister.Clear();
         m_ItemsToUnregister.Clear();
         m_IdsToFill.Clear();
         m_ItemToIndexTable.Clear();
         m_IndexToItemTable.Clear();
         m_DirtyItems.Clear();
     }
}