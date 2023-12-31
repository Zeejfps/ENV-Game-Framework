using System.Numerics;

namespace Tetris;

public sealed class TetrisSim
{
    private TetrisSimState m_TetrisSimState;

    private Vector2 m_TetrominoPosition;
    private float m_Time;
    private float m_TimeSinceLastTick;
    
    public TetrisSim()
    {
        m_TetrominoPosition = new Vector2(10, 20);
    }
    
    public TetrisSimState Save()
    {
        return new TetrisSimState
        {
            PlayState = PlayState.Playing,
            StaticMonominoStates = new []
            {
                new MonominoState
                {
                    Position = new Vector2(m_TetrominoPosition.X - 1f, m_TetrominoPosition.Y),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(m_TetrominoPosition.X, m_TetrominoPosition.Y),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(m_TetrominoPosition.X + 1f, m_TetrominoPosition.Y),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(m_TetrominoPosition.X, m_TetrominoPosition.Y + 1f),
                    Type = TetrominoType.I
                },
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
            MoveTetrominoDown();
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
        m_TetrominoPosition -= Vector2.UnitY;
    }

    public void DropTetrominoInstantly()
    {
        m_TetrominoPosition += Vector2.UnitY;
    }

    public void Pause()
    {
        
    }

    public void Resume()
    {
        
    }
}