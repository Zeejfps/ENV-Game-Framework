using System.Runtime.CompilerServices;

namespace SoftwareRendererModule;

public sealed class Bitmap
{
    public int Width { get; }
    public int Height { get; }
    public Span<uint> Pixels => _pixels;

    private readonly uint[] _pixels;

    public Bitmap(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new uint[width * height];
    }

    public void Fill(uint color)
    {
        Array.Fill(_pixels, color);
    }

    public void FillLine(int x, int y, int width, uint color)
    {
        var index = y * Width + x;
        Array.Fill(_pixels, color, index, width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPixel(int x, int y, uint color)
    {
        var index = y * Width + x;
        _pixels[index] = color;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPixel(int index, uint color)
    {
        _pixels[index] = color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public uint GetPixel(int x, int y)
    {
        var index = y * Width + x;
        return _pixels[index];
    }
}