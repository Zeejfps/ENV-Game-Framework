using System.Numerics;

namespace Tetris;

public sealed class CollisionGrid
{
    private readonly int m_Width;
    private readonly bool[] m_Grid;
    
    public CollisionGrid(int width, int height)
    {
        m_Grid = new bool[width * height];
    }

    public bool IsPositionOccupied(Vector2 position)
    {
        var x = (int)position.X;
        var y = (int)position.Y;

        if (x < 0 || y < 0)
            throw new Exception("Out of bounds");

        var index = x + y * m_Width;
        return m_Grid[index];
    }
}