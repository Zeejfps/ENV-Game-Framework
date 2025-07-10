using System.Runtime.CompilerServices;

namespace SoftwareRendererModule;

public sealed class Bitmap
{
    public int Width { get; }
    public int Height { get; }
    
    private readonly int[] _pixels;

    public Bitmap(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new int[width * height];
    }

    public void Fill(int color)
    {
        Array.Fill(_pixels, color);
    }

    public void FillLine(int x, int y, int width, int color)
    {
        var index = y * Width + x;
        Array.Fill(_pixels, color, index, width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPixel(int x, int y, int color)
    {
        var index = GetIndex(x, y);
        _pixels[index] = color;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPixel(int index, int color)
    {
        _pixels[index] = color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int GetPixel(int x, int y)
    {
        var index = GetIndex(x, y);
        return _pixels[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int GetIndex(int x, int y)
    {
        return Width * y + x;
    }
}