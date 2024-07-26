using System.Diagnostics;
using System.Numerics;
using OpenGlWrapper.Buffers;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class VertexArrayObjectManager
{
    private readonly ArrayBufferManager m_ArrayBufferManager;
    private VertexArrayObjectId m_BoundResource;

    public VertexArrayObjectManager(ArrayBufferManager arrayBufferManager)
    {
        m_ArrayBufferManager = arrayBufferManager;
    }
    
    public void Bind(VertexArrayObjectId handle)
    {
        if (handle == m_BoundResource)
            return;
        glBindVertexArray(handle);
        AssertNoGlError();
        m_BoundResource = handle;
    }
    
    public VertexArrayObjectId CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenVertexArrays(1, &id);
            AssertNoGlError();

            var handle = new VertexArrayObjectId(id);
            Bind(handle);

            return handle;
        }
    }

    public void Destroy(VertexArrayObjectId vao)
    {
        if (m_BoundResource == vao)
            Bind(VertexArrayObjectId.Null);
        
        unsafe
        {
            var id = vao.Id;
            glDeleteVertexArrays(1, &id);
            AssertNoGlError();
        }
    }

    public VertexArrayObjectManager EnableAndBindAttrib(int attribIndex, ArrayBufferId vbo, int size, GlType type, bool normalized, int stride, int offset)
    {
        BindAttrib(attribIndex, vbo, size, type, normalized, stride, offset);
        EnableAttrib(attribIndex);
        return this;
    }
    
    public VertexArrayObjectManager EnableAndBindAttrib(VertexArrayObjectTemplate template, int attribIndex, ArrayBufferId vbo)
    {
        var attrib = template.Attribs[attribIndex];
        BindAttrib(attrib.Index, vbo, attrib.Size, attrib.Type, attrib.Normalize, template.Stride, attrib.Offset);
        EnableAttrib(attrib.Index);
        return this;
    }

    public VertexArrayObjectManager EnableAttrib(int attribIndex)
    {
        AssertResourceIsBound();   
        glEnableVertexArrayAttrib(m_BoundResource, (uint)attribIndex);
        AssertNoGlError();
        return this;
    }

    public VertexArrayObjectManager BindAttrib(int attribIndex, ArrayBufferId vbo, int size, GlType type,
        bool normalized, int stride, int offset)
    {
        AssertResourceIsBound();   
        unsafe
        {
            m_ArrayBufferManager.Bind(vbo);
            glVertexAttribPointer((uint)attribIndex, size, (uint)type, normalized, stride, Offset(offset));
            AssertNoGlError();
            return this;
        }
    }

    public VertexArrayObjectTemplate CreateTemplate<T>() where T : unmanaged
    {
        unsafe
        {
            var type = typeof(T);
            var fields = type.GetFields();

            var attribs = new List<VertexArrayObjectAttribTemplate>();
            foreach (var field in fields)
            {
                var attribIndex = attribs.Count;
                var attribSize = 0;
                var attribType = GlType.Float;
            
                var fieldType = field.FieldType;
                if (fieldType == typeof(Vector2))
                {
                    attribType = GlType.Float;
                    attribSize = 2;
                }
                else if (fieldType == typeof(Vector3))
                {
                    attribType = GlType.Float;
                    attribSize = 3;
                }
                else if (fieldType == typeof(Vector4))
                {
                    attribType = GlType.Float;
                    attribSize = 4;
                }
                else if (fieldType == typeof(Matrix4x4))
                {
                    attribType = GlType.Float;
                    attribSize = 16;
                }
                else if (fieldType == typeof(Quaternion))
                {
                    attribType = GlType.Float;
                    attribSize = 4;
                }

                var attrib = new VertexArrayObjectAttribTemplate
                {
                    Index = attribIndex,
                    Size = attribSize,
                    Type = attribType,
                    Offset = AttribOffset<T>(field.Name)
                };
                attribs.Add(attrib);
            }
        
            return new VertexArrayObjectTemplate(sizeof(T), attribs);
        }
    }

    internal VertexArrayObjectMetadata GetMetadata(VertexArrayObjectId id)
    {
        return null;
    }

    [Conditional("DEBUG")]
    private void AssertResourceIsBound()
    {
        if (m_BoundResource == VertexArrayObjectId.Null)
            throw new InvalidOperationException("No resource bound");
    }
}

internal sealed class VertexArrayObjectMetadata
{
    
} 

public sealed class VertexArrayObjectTemplate
{
    public VertexArrayObjectTemplate(int stride) : this(stride, new List<VertexArrayObjectAttribTemplate>())
    {
        
    }
    
    public VertexArrayObjectTemplate(int stride, List<VertexArrayObjectAttribTemplate> attribs)
    {
        Stride = stride;
        m_Attribs = attribs;
    }

    private readonly List<VertexArrayObjectAttribTemplate> m_Attribs;

    public IReadOnlyList<VertexArrayObjectAttribTemplate> Attribs => m_Attribs;
    public int Stride { get; }
}

public sealed class VertexArrayObjectAttribTemplate
{
    public int Index { get; init; }
    public int Size { get; set; }
    public GlType Type { get; set; }
    public bool Normalize { get; set; }
    public int Offset { get; set; }
}

public readonly struct VertexArrayObjectId : IEquatable<VertexArrayObjectId>
{
    public static VertexArrayObjectId Null => new(0);
    
    internal uint Id { get; }

    public VertexArrayObjectId(uint id)
    {
        Id = id;
    }

    public bool Equals(VertexArrayObjectId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is VertexArrayObjectId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(VertexArrayObjectId left, VertexArrayObjectId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexArrayObjectId left, VertexArrayObjectId right)
    {
        return !left.Equals(right);
    }
    
    public static implicit operator uint(VertexArrayObjectId handle)
    {
        return handle.Id;
    }
}