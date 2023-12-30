using System.Numerics;

namespace Tetris;

public struct TetrisSimState
{
    public float Time;
    public PlayState PlayState;
    public TetrominoState TetrominoState;
    public MonominoState[] StaticMonominoStates;
}

public struct MonominoState
{
    public Vector2 Position;
    public TetrominoType Type;
}

public struct TetrominoState
{
    public Vector2 Position;
    public TetrominoType Type;
}

public enum TetrominoType
{
    I, O, S, Z, L, J
}

public enum PlayState
{
    Paused,
    Playing
}