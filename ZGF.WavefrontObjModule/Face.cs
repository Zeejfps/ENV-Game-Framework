using System.Text;

namespace ZGF.WavefrontObjModule;

public readonly struct Face
{
    public required Vertex[] Vertices { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append("f");

        foreach (var v in Vertices)
        {
            sb.Append(' ');
            sb.Append(v.PositionIndex);
            sb.Append('/');
            sb.Append(v.TextureCoordIndex);
            sb.Append('/');
            sb.Append(v.NormalIndex);
        }

        return sb.ToString();
    }
}