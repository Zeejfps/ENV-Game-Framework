using System.Runtime.InteropServices;
using System.Text;

namespace ZGF.WavefrontObjModule;

[StructLayout(LayoutKind.Sequential)]
public readonly struct VertexPosition
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
    public required float W { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append("f ");
        sb.Append(X);
        sb.Append(' ');
        sb.Append(Y);
        sb.Append(' ');
        sb.Append(Z);
        sb.Append(' ');

        if (Math.Abs(W - 1.0f) > 0.001f)
            sb.Append(W);

        return sb.ToString();
    }
}