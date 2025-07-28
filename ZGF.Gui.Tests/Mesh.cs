using System.Numerics;
using System.Runtime.InteropServices;
using OpenGL.NET;
using ZGF.WavefrontObjModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLBuffer;

namespace ZGF.Gui.Tests;

[StructLayout(LayoutKind.Sequential)]
public record struct VertexDefinition
{
    [VertexAttrib(3, typeof(float))]
    public Vector3 Position;

    [VertexAttrib(3, typeof(float))]
    public Vector3 Normal;
}

[StructLayout(LayoutKind.Sequential)]
public record struct TriangleDefinition
{
    public uint V0;
    public uint V1;
    public uint V2;
}

public sealed class MeshDefinition
{
    public required VertexDefinition[] Vertices { get; init; }
    public required TriangleDefinition[] Triangles { get; init; }
}

public sealed class Mesh
{
    public uint VaoId { get; init; }
    public uint VboId { get; }
    public uint IboId { get; }
    public int TriangleCount { get; init; }

    public static unsafe Mesh Upload(MeshDefinition mesh)
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();

        glBindVertexArray(vao);
        AssertNoGlError();

        uint vertexPositionBufferId, indexBufferId;
        glGenBuffers(1, &vertexPositionBufferId);
        AssertNoGlError();

        var vertexBuffer = glBindBuffer<VertexDefinition>(GL_ARRAY_BUFFER, vertexPositionBufferId);
        AssertNoGlError();

        glBufferData(vertexBuffer, mesh.Vertices.AsSpan(), BufferUsageHint.StaticDraw);
        AssertNoGlError();

        glVertexAttribPointer<VertexDefinition>(0, nameof(VertexDefinition.Position));
        AssertNoGlError();

        glVertexAttribPointer<VertexDefinition>(1, nameof(VertexDefinition.Normal));
        AssertNoGlError();

        glEnableVertexAttribArray(0);
        AssertNoGlError();

        glGenBuffers(1, &indexBufferId);
        AssertNoGlError();

        var indexBuffer = glBindBuffer<TriangleDefinition>(GL_ELEMENT_ARRAY_BUFFER, indexBufferId);
        AssertNoGlError();

        glBufferData(indexBuffer, mesh.Triangles.AsSpan(), BufferUsageHint.StaticDraw);
        AssertNoGlError();

        glBindVertexArray(0);

        return new Mesh
        {
            VaoId = vao,
            TriangleCount = mesh.Triangles.Length,
        };
    }

    public static Mesh LoadFromFile(string pathToMeshFile)
    {
        var objFileContents = WavefrontObj.ReadFromFile(pathToMeshFile);

        var helper = new VertexHelper(objFileContents);
        var triangles = new List<TriangleDefinition>();
        foreach (var face in objFileContents.Faces.Span)
        {
            var v0Index = helper.GetIndex(face.Vertices[0]);
            var v1Index = helper.GetIndex(face.Vertices[1]);
            var v2Index = helper.GetIndex(face.Vertices[2]);
            triangles.Add(new TriangleDefinition
            {
                V0 = v0Index,
                V1 = v1Index,
                V2 = v2Index,
            });
        }

        var meshDefinition = new MeshDefinition
        {
            Triangles = triangles.ToArray(),
            Vertices = helper.GetVertices(),
        };

        return Upload(meshDefinition);
    }
    
}