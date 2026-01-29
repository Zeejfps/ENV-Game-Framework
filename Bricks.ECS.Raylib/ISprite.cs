using System.Numerics;

namespace Bricks.ECS.Raylib;

interface ISprite
{
    void DrawSelf();
    Vector2 Pos { get; set; }
}