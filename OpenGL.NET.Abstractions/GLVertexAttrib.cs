using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGL.NET.Abstractions;

public static class GLVertexAttrib
{
    public static unsafe void glVertexAttribPointer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(
        uint attribIndex,
        string fieldName,
        bool normalize = false) where TVertex : unmanaged
    {
        var vertexType = typeof(TVertex);
        if (vertexType == null)
            throw new Exception("Failed to get vertex type");

        var field = vertexType.GetField(fieldName);
        if (field == null)
            throw new Exception($"Field {fieldName} not found on type {vertexType}");

        var vertexAttrib = field.GetCustomAttribute<VertexAttribAttribute>();
        if (vertexAttrib == null)
            throw new Exception($"Field {fieldName} must have a {nameof(VertexAttribAttribute)} attribute");

        var offset = FieldOffset<TVertex>(fieldName);
        var glType = GetGlType(vertexAttrib.Type, out var typeSize);
        var componentCount = vertexAttrib.Count;
        var ptrOffset = new IntPtr(offset);
        var strideInBytes = Marshal.SizeOf<TVertex>();
        Console.WriteLine($"Field: {fieldName}");
        Console.WriteLine($"Component Count: {componentCount}");
        Console.WriteLine($"Offset: {offset}");
        Console.WriteLine($"Stride in bytes: {strideInBytes}");

        GL46.glVertexAttribPointer(
            attribIndex,
            componentCount,
            glType,
            normalize,
            strideInBytes,
            ptrOffset.ToPointer()
        );
    }
}
