using System.Diagnostics;
using System.Reflection;

namespace OpenGLSandbox;

using static OpenGlUtils;
using static GL46;

public sealed unsafe class VertexAttribInstanceBuffer<TInstancedData> where TInstancedData : unmanaged
{
    private readonly HashSet<IInstancedItem<TInstancedData>> m_ItemsToRegister = new();
    private readonly HashSet<IInstancedItem<TInstancedData>> m_ItemsToUnregister = new();
    private readonly HashSet<IInstancedItem<TInstancedData>> m_DirtyItems = new();
    private readonly Dictionary<IInstancedItem<TInstancedData>, int> m_ItemToIndexTable = new();
    private readonly Dictionary<int, IInstancedItem<TInstancedData>> m_IndexToItemTable = new();
    private readonly SortedSet<int> m_EmptyIndices = new();
    private readonly uint m_MaxInstanceCount;
    private readonly uint m_VertexAttribIndexOffset;
    
    private uint m_BufferId;
    private int m_ItemCount;

    public int ItemCount => m_ItemCount;

    public VertexAttribInstanceBuffer(uint vertexAttribIndexOffset, uint maxInstancesCount)
    {
        m_VertexAttribIndexOffset = vertexAttribIndexOffset;
        m_MaxInstanceCount = maxInstancesCount;
    }

    public void Alloc()
    {
        var maxInstancesCount = m_MaxInstanceCount;
        
        m_BufferId = glGenBuffer();
        glBindBuffer(GL_ARRAY_BUFFER, m_BufferId);
        glBufferData(GL_ARRAY_BUFFER, SizeOf<TInstancedData>(maxInstancesCount), (void*)0, GL_DYNAMIC_DRAW);

        var instancedDataType = typeof(TInstancedData);
        var fields = instancedDataType.GetFields()
            .Where(fieldInfo => fieldInfo.GetCustomAttribute<InstancedAttrib>() != null);
        
        uint attribIndex = m_VertexAttribIndexOffset;
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
            glEnableVertexAttribArray(attribIndex);
            glVertexAttribDivisor(attribIndex, 1);
            attribIndex++;
        }
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
        UnregisterItems();
        RegisterItems();
        FillEmptyIndices();
        UploadDataToGpu();
    }

    public void Free()
    {
        var id = m_BufferId;
        glDeleteBuffers(1, &id);
        AssertNoGlError();
        
        m_ItemCount = 0;
        m_ItemsToRegister.Clear();
        m_ItemsToUnregister.Clear();
        m_EmptyIndices.Clear();
        m_ItemToIndexTable.Clear();
        m_IndexToItemTable.Clear();
        m_DirtyItems.Clear();
    }

    private void UnregisterItems()
    {
        if (m_ItemsToUnregister.Count == 0)
            return;
        
        foreach (var item in m_ItemsToUnregister)
        {
            item.BecameDirty -= Item_OnBecameDirty;
            var index = m_ItemToIndexTable[item];
            m_EmptyIndices.Add(index);
            m_IndexToItemTable.Remove(index);
            m_ItemToIndexTable.Remove(item);
            m_DirtyItems.Remove(item);
        }
        m_ItemsToUnregister.Clear();
    }

    private void RegisterItems()
    {
        if (m_ItemsToRegister.Count == 0)
            return;
        
        foreach (var item in m_ItemsToRegister)
        {
            item.BecameDirty += Item_OnBecameDirty;
            int index;
            if (m_EmptyIndices.Count > 0)
            {
                index = m_EmptyIndices.Min;
                m_EmptyIndices.Remove(index);
            }
            else
            {
                index = m_ItemCount;
                m_ItemCount++;
            }

            UpdateItemIndexLookup(item, index);
                
            m_DirtyItems.Add(item);
        }
        m_ItemsToRegister.Clear();
    }

    // TODO: Better name?
    private void FillEmptyIndices()
    {
        foreach (var emptyIndex in m_EmptyIndices.Reverse())
        {
            var lastItemId = m_ItemCount - 1;
            if (emptyIndex != lastItemId)
            {
                //Console.WriteLine($"Moving last panel into an id we need to fill. Id: {idToFill}");
                var lastItem = m_IndexToItemTable[lastItemId];
                m_IndexToItemTable.Remove(lastItemId);
                
                // NOTE(Zee): Not needed here because we are overriding the index in the UpdateItemIndexLookup method
                //m_ItemToIndexTable.Remove(lastItem);
                
                UpdateItemIndexLookup(lastItem, emptyIndex);

                m_DirtyItems.Add(lastItem);
            }
                
            m_ItemCount--;
        }
        m_EmptyIndices.Clear();
    }

    private void UploadDataToGpu()
    {
        if (m_DirtyItems.Count == 0)
            return;

        var minIndex = int.MaxValue;
        var maxIndex = 0;
        foreach (var dirtyItem in m_DirtyItems)
        {
            var index = m_ItemToIndexTable[dirtyItem];
            if (index > maxIndex)
                maxIndex = index;
            if (index < minIndex)
                minIndex = index;
        }
        var bufferLength = (maxIndex + 1) - minIndex;
        //Console.WriteLine($"Mapping Buffer Range: {minIndex} -> {maxIndex}, Length: {bufferLength}");
        
        glBindBuffer(GL_ARRAY_BUFFER, m_BufferId);
        AssertNoGlError();
        var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, SizeOf<TInstancedData>(minIndex), SizeOf<TInstancedData>(bufferLength), GL_MAP_WRITE_BIT);
        AssertNoGlError();
        var buffer = new Span<TInstancedData>(bufferPtr, bufferLength);

        foreach (var dirtyItem in m_DirtyItems)
        {
            var dirtyItemIndex = m_ItemToIndexTable[dirtyItem];
            var dstIndex = dirtyItemIndex - minIndex;

            // NOTE(Zee): Why am I doing this?
            // I think its because I want the items that change frequently to be at the begging of the 
            // buffer, that way we don't have to map a large portion of the buffer?
            // if (dirtyItemIndex > dstIndex)
            // {
            //     Console.WriteLine($"Swaping {dirtyItemIndex} with {dstIndex}");
            //     var srcIndex = dirtyItemIndex;
            //     var dstItem = m_IndexToItemTable[dstIndex];
            //     var dstItemData = buffer[dstIndex];
            //     
            //     buffer[srcIndex] = dstItemData;
            //     
            //     UpdateItemIndexLookup(dstItem, srcIndex);
            //     UpdateItemIndexLookup(dirtyItem, dstIndex);
            // }

            dirtyItem.UpdateInstanceData(ref buffer[dstIndex]);
            //Console.WriteLine(buffer[dstIndex]);
        }
        //Console.WriteLine($"Updated Dirty Items: {m_DirtyItems.Count}");
        m_DirtyItems.Clear();
        glUnmapBuffer(GL_ARRAY_BUFFER);

        //Console.WriteLine($"Dirty Count: {m_DirtyCount}, Panel Count: {m_PanelCount}");
    }

    private void UpdateItemIndexLookup(IInstancedItem<TInstancedData> item, int index)
    {
        m_IndexToItemTable[index] = item;
        m_ItemToIndexTable[item] = index;
    }

    private void Item_OnBecameDirty(IInstancedItem<TInstancedData> item)
    {
        m_DirtyItems.Add(item);
    }
}