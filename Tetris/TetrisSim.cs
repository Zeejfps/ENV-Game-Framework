namespace Tetris;

public sealed class TetrisSim
{
    private TetrisSimState m_TetrisSimState;

    public TetrisSimState Save()
    {
        return m_TetrisSimState;
    }

    public void Load(TetrisSimState state)
    {
        m_TetrisSimState = state;
    }
    
    public void Update(float dt)
    {
        
    }

    public void MoveTetrominoRight()
    {
        
    }

    public void MoveTetrominoLeft()
    {
        
    }

    public void MoveTetrominoDown()
    {
        
    }

    public void DropTetrominoInstantly()
    {
        
    }

    public void Pause()
    {
        
    }

    public void Resume()
    {
        
    }
}