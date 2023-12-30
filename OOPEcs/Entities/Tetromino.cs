using Tetris;

namespace Entities;

public interface ITetrominoShape
{
    IEnumerable<Monomino> Monominos { get; }
}

public sealed class Tetromino : Entity
{
    private readonly ITetrominoShape m_Shape;
    
    public Tetromino(ITetrominoShape shape)
    {
        m_Shape = shape;
    }
    
    public bool TryMoveDown()
    {
        var monominos = m_Shape.Monominos;
        foreach (var monomino in monominos)
            monomino.MoveDown();

        return false;
    }

    protected override void OnDispose(bool disposing)
    {
        
    }
}