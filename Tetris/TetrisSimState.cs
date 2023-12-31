using System.Numerics;

namespace Tetris;

public sealed class TetrisSimState
{
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
    public Vector2[] Offsets;
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