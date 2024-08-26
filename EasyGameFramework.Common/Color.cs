using System.Runtime.InteropServices;

namespace OpenGLSandbox;

[StructLayout(LayoutKind.Sequential)]
public struct Color : IEquatable<Color>
{
    public float R;
    public float G;
    public float B;
    public float A;

    public Color(float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color FromHex(uint color, float alpha)
    {
        var red = ((color >> 16) & 0xFF) / 255f;    // Extract red
        var green = ((color >> 8) & 0xFF) / 255f;   // Extract green
        var blue = ((color >> 0) & 0xFF) / 255f;

        return new Color()
        {
            R = red,
            G = green,
            B = blue,
            A = alpha,
        };
    }

    public override string ToString()
    {
        return $"{nameof(R)}: {R}, {nameof(G)}: {G}, {nameof(B)}: {B}, {nameof(A)}: {A}";
    }

    public bool Equals(Color other)
    {
        return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
    }

    public override bool Equals(object? obj)
    {
        return obj is Color other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
    }
}