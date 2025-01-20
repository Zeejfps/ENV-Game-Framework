using System.Runtime.CompilerServices;

namespace Bricks;

public static class CoordinateSystem
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBelow(this float y, float otherY)
    {
        return y < otherY;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAbove(this float y, float otherY)
    {
        return y < otherY;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeft(this float x, float otherX)
    {
        return x < otherX;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRight(this float x, float otherX)
    {
        return x > otherX;
    }
}