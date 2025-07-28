using System.Numerics;
using ZGF.WavefrontObjModule;

namespace ZGF.Gui.Tests;

public ref struct VertexHelper
{
    private readonly Dictionary<VertexDefinition, uint> _verticesIndexByDefinitionLookup = new();
    private readonly List<VertexDefinition> _vertices = new();
    private readonly ReadOnlySpan<VertexPosition> _vertexPositions;
    private readonly ReadOnlySpan<VertexNormal> _vertexNormals;
    private readonly ReadOnlySpan<VertexTextureCoord> _vertexTextureCoords;

    public VertexHelper(IWavefrontObjFileContents contents)
    {
        _vertexPositions = contents.VertexPositions.Span;
        _vertexNormals = contents.VertexNormals.Span;
        _vertexTextureCoords = contents.VertexTextureCoords.Span;
    }

    private Vector3 ToVector(in VertexPosition vertexPosition)
    {
        return new Vector3(vertexPosition.X, vertexPosition.Y, vertexPosition.Z);
    }
    
    private Vector3 ToVector(in VertexNormal vertexNormal)
    {
        return new Vector3(vertexNormal.X, vertexNormal.Y, vertexNormal.Z);
    }

    private Vector2 ToVector(in VertexTextureCoord textureCoord)
    {
        return new Vector2(textureCoord.U, textureCoord.V);
    }

    private VertexDefinition ToVertexDefinition(in Vertex v)
    {
        return new VertexDefinition
        {
            Position = ToVector(_vertexPositions[v.PositionIndex-1]),
            Normal = ToVector(_vertexNormals[v.NormalIndex-1]),
        };
    }

    private uint GetIndex(in VertexDefinition vertex)
    {
        if (!_verticesIndexByDefinitionLookup.TryGetValue(vertex, out var index))
        {
            index = (uint)_vertices.Count;
            _verticesIndexByDefinitionLookup[vertex] = index;
            _vertices.Add(vertex);
        }
        return index;
    }

    public uint GetIndex(in Vertex v)
    {
        var def =  ToVertexDefinition(v);
        return GetIndex(def);
    }

    public VertexDefinition[] GetVertices()
    {
        return _vertices.ToArray();
    }
}