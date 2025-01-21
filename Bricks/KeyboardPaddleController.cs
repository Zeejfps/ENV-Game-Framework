using Bricks.Archetypes;
using Bricks.Entities;

namespace Bricks;

public sealed class KeyboardPaddleController : IPaddleController
{
    private IKeyboard Keyboard { get; }

    public KeyboardPaddleController(IKeyboard keyboard)
    {
        Keyboard = keyboard;
    }

    public void ApplyInputs(IPaddle paddle)
    {
        if (Keyboard.IsKeyDown(KeyCode.A))
        {
            paddle.MoveLeft();
        }
        if (Keyboard.IsKeyDown(KeyCode.D))
        {
            paddle.MoveRight();
        }
    }
}