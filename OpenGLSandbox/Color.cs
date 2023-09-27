namespace OpenGLSandbox;

public struct Color
{
    public float R;
    public float G;
    public float B;
    public float A;

    public static Color FromHex(int color, float alpha)
    {
        var red = (color >> 16) & 0xFF;    // Extract red
        var green = (color >> 8) & 0xFF;   // Extract green
        var blue = color & 0xFF;

        return new Color()
        {
            R = red,
            G = green,
            B = blue,
            A = alpha,
        };
    }
}