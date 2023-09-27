using System.Runtime.InteropServices;

namespace OpenGLSandbox;

[StructLayout(LayoutKind.Sequential)]
public struct Color
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
}