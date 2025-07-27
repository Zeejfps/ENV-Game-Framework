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

    private static Vector3 ToVector(in VertexPosition vertexPosition)
    {
        return new Vector3(vertexPosition.X, vertexPosition.Y, vertexPosition.Z);
    }

    private static Vector2 ToVector(in VertexTextureCoord textureCoord)
    {
        return new Vector2(textureCoord.U, textureCoord.V);
    }

    private static VertexDefinition ToVertexDefinition(
        in Vertex v,
        ReadOnlySpan<VertexPosition> positions,
        ReadOnlySpan<VertexTextureCoord> textureCoords)
    {
        return new VertexDefinition
        {
            Position = ToVector(positions[v.PositionIndex-1]),
            UVs = ToVector(textureCoords[v.TextureCoordIndex-1]),
        };
    }

    public static Mesh LoadFromFile(string pathToMeshFile)
    {
        var objFileContents = WavefrontObj.ReadFromFile(pathToMeshFile);

        var vertices = new Dictionary<VertexDefinition, int>();
        var triangles = new List<TriangleDefinition>();
        var positions = objFileContents.VertexPositions.Span;
        var textureCoords = objFileContents.VertexTextureCoords.Span;
        foreach (var face in objFileContents.Faces.Span)
        {
            var v0 = ToVertexDefinition(face.Vertices[0], positions, textureCoords);
            var v1 = ToVertexDefinition(face.Vertices[1], positions, textureCoords);
            var v2 = ToVertexDefinition(face.Vertices[2], positions, textureCoords);

            if (!vertices.TryGetValue(v0, out var v0Index))
            {
                v0Index = vertices.Count;
                vertices[v0] = v0Index;
            }

            if (!vertices.TryGetValue(v1, out var v1Index))
            {
                v1Index = vertices.Count;
                vertices[v1] = v1Index;
            }

            if (!vertices.TryGetValue(v2, out var v2Index))
            {
                v2Index = vertices.Count;
                vertices[v2] = v2Index;
            }
            
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
            Vertices = vertices.Keys.ToArray(),
        };

        return Upload(meshDefinition);
    }
    
}