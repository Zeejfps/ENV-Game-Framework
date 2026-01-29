using System.Numerics;
using Raylib_CsLo;

namespace Bricks.ECS.Raylib;

class BallSprite : ISprite
{
    public Vector2 Pos { get; set; }
    
    private Texture _spriteSheet;

    private float _width;
    private float _height;
    private float _halfWidth;
    private float _halfHeight;
    private readonly Color _tint = new(255, 255, 255, 255);


    public BallSprite(Texture spriteSheet, float width, float height)
    {
        _spriteSheet = spriteSheet;
        _width = width;
        _height = height;
        _halfWidth = width * 0.5f;
        _halfHeight = height * 0.5f;
    }
    
    public void DrawSelf()
    {
        var left = Pos.X - _halfWidth;
        var top = Pos.Y + _halfHeight;
        Raylib_CsLo.Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(120, 0, 20, 20),
            new Rectangle(left, top, _width, _height),
            new Vector2(0, 0),
            0, 
            _tint);
    }
}