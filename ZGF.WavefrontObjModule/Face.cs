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
            if (v.PositionIndex != 0)
                sb.Append(v.PositionIndex);
            sb.Append('/');
            if (v.TextureCoordIndex != 0)
                sb.Append(v.TextureCoordIndex);
            sb.Append('/');
            if (v.NormalIndex != 0)
                sb.Append(v.NormalIndex);
        }

        return sb.ToString();
    }
}