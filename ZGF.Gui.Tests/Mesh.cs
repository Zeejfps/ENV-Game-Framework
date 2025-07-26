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

    [VertexAttrib(2, typeof(float))]
    public Vector2 UVs;
}

[StructLayout(LayoutKind.Sequential)]
public record struct TriangleDefinition
{
    public int V0;
    public int V1;
    public int V2;
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

    public static unsafe Mesh Upload(MeshDefinition meshDefinition)
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();

        glBindVertexArray(vao);
        AssertNoGlError();

        uint vertexPositionBufferId, indexBufferId;
        glGenBuffers(1, &vertexPositionBufferId);
        AssertNoGlError();

        var vertexPositionBuffer = glBindBuffer<VertexDefinition>(GL_ARRAY_BUFFER, vertexPositionBufferId);
        AssertNoGlError();

        glBufferData(vertexPositionBuffer, meshDefinition.Vertices.AsSpan(), BufferUsageHint.StaticDraw);
        AssertNoGlError();

        glVertexAttribPointer<VertexDefinition>(0, nameof(VertexDefinition.Position));
        AssertNoGlError();

        glVertexAttribPointer<VertexDefinition>(1, nameof(VertexDefinition.UVs));
        AssertNoGlError();

        glEnableVertexAttribArray(0);
        AssertNoGlError();

        glGenBuffers(1, &indexBufferId);
        AssertNoGlError();

        var indexBuffer = glBindBuffer<TriangleDefinition>(GL_ELEMENT_ARRAY_BUFFER, indexBufferId);
        AssertNoGlError();

        glBufferData(indexBuffer, meshDefinition.Triangles.AsSpan(), BufferUsageHint.StaticDraw);
        AssertNoGlError();

        glBindVertexArray(0);
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);

        return new Mesh
        {
            VaoId = vao,
        };
    }

    public static Mesh LoadFromFile(string pathToMeshFile)
    {
        var objFileContents = WavefrontObj.ReadFromFile(pathToMeshFile);

        var remap = new Dictionary<Vertex, int>();

        var vertexPositions = objFileContents.VertexPositions.Span;
        var vertices = new VertexDefinition[vertexPositions.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var position = vertexPositions[i];
            var uv = objFileContents.VertexTextureCoords.Span[i];
            vertices[i] = new VertexDefinition
            {
                Position = new Vector3(position.X, position.Y, position.Z),
                UVs = new Vector2(uv.U, uv.V)
            };
        }

        var triangles = new TriangleDefinition[objFileContents.Faces.Length];
        for (var i = 0; i < triangles.Length; i++)
        {
            var face = objFileContents.Faces.Span[i];
            var v0 = face.Vertices[0];
            if (!remap.TryGetValue(v0, out var v0Index))
            {
                var pos = vertexPositions[v0.PositionIndex - 1];
                var v = new VertexDefinition
                {
                    Position = new Vector3(pos.X, pos.Y, pos.Z),
                };
                v0Index = vertices.Length;
                remap[v0] = v0Index;
            }

            var v1 = face.Vertices[1];
            var v2 = face.Vertices[2];
            triangles[i] = new TriangleDefinition
            {
                V0 = v0.PositionIndex-1,
                V1 = v1.PositionIndex-1,
                V2 = v2.PositionIndex-1
            };
        }

        var meshDefinition = new MeshDefinition
        {
            Triangles = triangles,
            Vertices = vertices,
        };

        return Upload(meshDefinition);
    }
    
}