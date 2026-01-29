using System.Numerics;
using Raylib_CsLo;

namespace Bricks.ECS.Raylib;

class PaddleSprite : ISprite
{
    private readonly Texture _spriteSheet;
    private readonly float _width;
    private readonly float _height;
    private readonly Color _tint = new(255, 255, 255, 255);

    public PaddleSprite(Texture spriteSheet, float width, float height)
    {
        _spriteSheet = spriteSheet;
        _width = width;
        _height = height;
    }

    public void DrawSelf()
    {
        Raylib_CsLo.Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(0, 0, 120, 19),
            new Rectangle(Pos.X - _width * 0.5f, Pos.Y - _height * 0.5f, _width, _height),
            new Vector2(0, 0),
            0, 
            _tint);
    }

    public Vector2 Pos { get; set; }
}