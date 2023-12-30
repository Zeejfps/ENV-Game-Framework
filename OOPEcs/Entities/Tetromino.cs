using System.Numerics;
using EasyGameFramework.Api;
using Tetris;

namespace Entities;

public interface ITetrominoShape
{
    IEnumerable<Monomino> Monominos { get; }
}

public sealed class Tetromino : IEntity
{
    private readonly IClock m_Clock;
    private readonly ITetrominoShape m_Shape;
    
    public Tetromino(IClock clock, ITetrominoShape shape)
    {
        m_Clock = clock;
        m_Shape = shape;
    }

    private float m_Time;
    
    public void Load()
    {
        m_Clock.Ticked += Clock_OnTicked;
    }

    public void Unload()
    {
        m_Clock.Ticked -= Clock_OnTicked;
    }

    private void Clock_OnTicked()
    {
        m_Time += m_Clock.DeltaTime;
        if (m_Time > 1f)
        {
            MoveDown();
            m_Time = 0f;
        }
    }

    private void MoveDown()
    {
        var monominos = m_Shape.Monominos;
        foreach (var monomino in monominos)
            monomino.MoveDown();
    }
}