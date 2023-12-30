using Tetris;

namespace Entities;

public sealed class Grid : IEntity
{
    private readonly Context m_Context;
    private readonly List<Monomino> m_Monominos;
    
    public Grid(Context parentContext)
    {
        m_Context = new Context(parentContext);
        m_Monominos = new();
    }

    public void Load()
    {
        SpawnTetromino();
    }

    public void Unload()
    {
        foreach (var monomino in m_Monominos)
            monomino.Unload();
        m_Monominos.Clear();
    }

    private void SpawnTetromino()
    {
        //var monomino = m_Context.
    }
}