using System.Numerics;

namespace Tetris;

public sealed class TetrisSim
{
    private Vector2 m_TetrominoPosition;
    private float m_TimeSinceLastTick;

    private CollisionGrid m_CollisionGrid;
    private List<Vector2> m_Offsets;
    private List<MonominoState> m_StaticMonominos = new();
    
    public TetrisSim()
    {
        m_CollisionGrid = new CollisionGrid(10, 20);
        m_TetrominoPosition = new Vector2(10, 20);
        m_Offsets = new List<Vector2>
        {
            new(-1f, 0f),
            new(0f, 0f),
            new(1f, 0f),
            new(0f, 1f),
        };
    }
    
    public void SaveTo(TetrisSimState state)
    {
        state.PlayState = PlayState.Playing;
        state.StaticMonominoStates = m_StaticMonominos.ToArray();
        state.TetrominoState = new TetrominoState
        {
            Position = m_TetrominoPosition,
            Offsets = m_Offsets.ToArray(),
            Type = TetrominoType.I
        };
    }

    public void LoadFrom(TetrisSimState state)
    {
    }
    
    public void Update(float dt)
    {
        m_TimeSinceLastTick += dt;
        if (m_TimeSinceLastTick > 1f)
        {
            m_TimeSinceLastTick = 0f;
            if (!TryMoveTetrominoDown())
            {
                OnTetrominoLanded();
                SpawnTetromino();
            }
        }
    }

    public void MoveTetrominoRight()
    {
        m_TetrominoPosition += Vector2.UnitX;
    }

    public void MoveTetrominoLeft()
    {
        m_TetrominoPosition -= Vector2.UnitX;
    }

    public void MoveTetrominoDown()
    {
        TryMoveTetrominoDown();
    }

    public void DropTetrominoInstantly()
    {
        while (TryMoveTetrominoDown()) {}
    }

    public void Pause()
    {
        
    }

    public void Resume()
    {
        
    }

    private bool TryMoveTetrominoDown()
    {
        var nextPosition = m_TetrominoPosition - Vector2.UnitY;
        foreach (var offset in m_Offsets)
        {
            var monominoPosition = nextPosition + offset;
            if (monominoPosition.Y < 0)
                return false;

            if (m_CollisionGrid.IsPositionOccupied(monominoPosition))
                return false;
        }
        
        if (nextPosition.Y <= 0)
            return false;

        m_TetrominoPosition = nextPosition;
        return true;
    }

    private void SpawnTetromino()
    {
        m_TetrominoPosition = new Vector2(10f, 20f);
    }

    private void OnTetrominoLanded()
    {
        foreach (var offset in m_Offsets)
        {
            m_StaticMonominos.Add(new MonominoState
            {
                Position = m_TetrominoPosition + offset,
                Type = TetrominoType.I
            });
        }
    }
}