using System.Numerics;

namespace Tetris;

public sealed class TetrisSim
{
    private TetrisSimState m_TetrisSimState;

    private Vector2 m_TetrominoPosition;
    private float m_Time;
    private float m_TimeSinceLastTick;

    private List<Vector2> m_Offsets;
    private List<MonominoState> m_StaticMonominos = new();
    
    public TetrisSim()
    {
        m_TetrominoPosition = new Vector2(10, 20);
        m_Offsets = new List<Vector2>
        {
            new(-1f, 0f),
            new(0f, 0f),
            new(1f, 0f),
            new(0f, 1f),
        };
    }
    
    public TetrisSimState Save()
    {
        return new TetrisSimState
        {
            PlayState = PlayState.Playing,
            StaticMonominoStates = m_StaticMonominos.ToArray(),
            // StaticMonominoStates = new []
            // {
            //     new MonominoState
            //     {
            //         Position = new Vector2(m_TetrominoPosition.X - 1f, m_TetrominoPosition.Y),
            //         Type = TetrominoType.I
            //     },
            //     new MonominoState
            //     {
            //         Position = new Vector2(m_TetrominoPosition.X, m_TetrominoPosition.Y),
            //         Type = TetrominoType.I
            //     },
            //     new MonominoState
            //     {
            //         Position = new Vector2(m_TetrominoPosition.X + 1f, m_TetrominoPosition.Y),
            //         Type = TetrominoType.I
            //     },
            //     new MonominoState
            //     {
            //         Position = new Vector2(m_TetrominoPosition.X, m_TetrominoPosition.Y + 1f),
            //         Type = TetrominoType.I
            //     },
            // },
            TetrominoState = new TetrominoState
            {
                Position = m_TetrominoPosition,
                Offsets = m_Offsets.ToArray(),
                Type = TetrominoType.I
            }
        };
    }

    public void Load(TetrisSimState state)
    {
        m_TetrisSimState = state;
    }
    
    public void Update(float dt)
    {
        m_Time += dt;
        m_TimeSinceLastTick += dt;
        if (m_TimeSinceLastTick > 1f)
        {
            m_TimeSinceLastTick = 0f;
            if (!TryMoveTetrominoDown())
            {
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
        if (nextPosition.Y <= 0)
            return false;

        m_TetrominoPosition = nextPosition;
        return true;
    }

    private void SpawnTetromino()
    {
        foreach (var offset in m_Offsets)
        {
            m_StaticMonominos.Add(new MonominoState
            {
                Position = m_TetrominoPosition + offset,
                Type = TetrominoType.I
            });
        }

        m_TetrominoPosition = new Vector2(10f, 20f);
    }
}