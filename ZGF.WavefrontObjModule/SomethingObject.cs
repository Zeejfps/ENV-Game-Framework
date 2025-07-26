namespace ZGF.WavefrontObjModule;

internal sealed class SomethingObject
{
    public required string Name { get; init; }

    public Range VertexPositions { get; set; }

    public int VertexNormalsIndex { get; set; }
    public int VertexNormalsCount { get; set; }

    public int VertexTextureCoordsIndex { get; set; }
    public int VertexTextureCoordsCount { get; set; }

    public int FacesIndex { get; set; }
    public int FacesCount { get; set; }

    public void SetVertexNormalsRange(int startIndex, int count)
    {
        VertexNormalsIndex = startIndex;
        VertexNormalsCount = count;
    }

    public void SetVertexTextureCoordsRange(int startIndex, int count)
    {
        VertexTextureCoordsIndex = startIndex;
        VertexTextureCoordsCount = count;
    }

    public void SetFacesRange(int startIndex, int count)
    {
        FacesIndex = startIndex;
        FacesCount = count;
    }
}