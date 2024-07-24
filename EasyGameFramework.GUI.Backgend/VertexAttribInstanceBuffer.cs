using System.Diagnostics;
using System.Reflection;

namespace OpenGLSandbox;

using static OpenGlUtils;
using static GL46;

public sealed unsafe class VertexAttribInstanceBuffer<TInstancedData> where TInstancedData : unmanaged
{
    private readonly HashSet<IEntity<TInstancedData>> m_ItemsToRegister = new();
    private readonly HashSet<IEntity<TInstancedData>> m_ItemsToUnregister = new();
    private readonly HashSet<IEntity<TInstancedData>> m_DirtyItems = new();
    private readonly Dictionary<IEntity<TInstancedData>, int> m_ItemToIndexTable = new();
    private readonly Dictionary<int, IEntity<TInstancedData>> m_IndexToItemTable = new();
    private readonly SortedSet<int> m_EmptyIndices = new();
    private readonly uint m_MaxInstanceCount;
    private readonly uint m_VertexAttribIndexOffset;
    
    private int m_ItemCount;
    public int ItemCount => m_ItemCount;

    private readonly ArrayBuffer<TInstancedData> m_Buffer = new();

    public VertexAttribInstanceBuffer(uint vertexAttribIndexOffset, uint maxInstancesCount)
    {
        m_VertexAttribIndexOffset = vertexAttribIndexOffset;
        m_MaxInstanceCount = maxInstancesCount;
    }

    public void Alloc()
    {
        var maxInstancesCount = m_MaxInstanceCount;
        m_Buffer.Alloc((int)maxInstancesCount, MutableBufferUsageHints.DynamicDraw);
        
        var instancedDataType = typeof(TInstancedData);
        var fields = instancedDataType.GetFields()
            .Where(fieldInfo => fieldInfo.GetCustomAttribute<VertexAttrib>() != null);
        
        uint attribIndex = m_VertexAttribIndexOffset;
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<VertexAttrib>();
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

    public void Add(IEntity<TInstancedData> item)
    {
        m_ItemsToRegister.Add(item);
        m_ItemsToUnregister.Remove(item);
    }

    public void Remove(IEntity<TInstancedData> item)
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
        m_Buffer.Free();
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
        
        m_Buffer.WriteMapped(minIndex, bufferLength, gpuMemory =>
        {
            var buffer = gpuMemory.Span;
            foreach (var dirtyItem in m_DirtyItems)
            {
                var dirtyItemIndex = m_ItemToIndexTable[dirtyItem];
                var dstIndex = dirtyItemIndex - minIndex;
                ref var test = ref buffer[dstIndex];
                dirtyItem.LoadComponent(ref test);
            }
            m_DirtyItems.Clear();
        });
    }

    private void UpdateItemIndexLookup(IEntity<TInstancedData> item, int index)
    {
        m_IndexToItemTable[index] = item;
        m_ItemToIndexTable[item] = index;
    }

    private void Item_OnBecameDirty(IEntity<TInstancedData> item)
    {
        m_DirtyItems.Add(item);
    }
}