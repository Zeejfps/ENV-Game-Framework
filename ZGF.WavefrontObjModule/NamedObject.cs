namespace ZGF.WavefrontObjModule;

internal sealed class NamedObject : INamedObject
{
    private int _vertexPositionsIndex;
    private int _vertexPositionsCount;
    
    private int _vertexNormalsIndex;
    private int _vertexNormalsCount;
    
    private int _vertexTextureCoordsIndex;
    private int _vertexTextureCoordsCount;

    private int _facesIndex;
    private int _facesCount;
    
    public required WavefrontObjFileContents Context { get; init; }
    public required string Name { get; init; }
    
    public ReadOnlySpan<VertexPosition> VertexPositions => Context
        .vertexPositions
        .AsSpan(_vertexPositionsIndex, _vertexPositionsCount);
    
    public ReadOnlySpan<VertexNormal> VertexNormals => Context
        .vertexNormals
        .AsSpan(_vertexNormalsIndex, _vertexNormalsCount);
    
    public ReadOnlySpan<VertexTextureCoord> VertexTextureCoords => Context
        .vertexTextureCoords
        .AsSpan(_vertexTextureCoordsIndex, _vertexTextureCoordsCount);
    
    public ReadOnlySpan<Face> Faces => Context
        .faces
        .AsSpan(_vertexNormalsIndex, _vertexNormalsCount);

    public void SetVertexPositionRange(int startIndex, int length)
    {
        _vertexPositionsIndex = startIndex;
        _vertexPositionsCount = length;
    }

    public void SetVertexNormalsRange(int startIndex, int count)
    {
        _vertexNormalsIndex = startIndex;
        _vertexNormalsCount = count;
    }

    public void SetVertexTextureCoordsRange(int startIndex, int count)
    {
        _vertexTextureCoordsIndex = startIndex;
        _vertexTextureCoordsCount = count;
    }

    public void SetFacesRange(int startIndex, int count)
    {
        _facesIndex = startIndex;
        _facesCount = count;
    }
}