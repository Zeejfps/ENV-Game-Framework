using Bricks.Archetypes;

namespace Bricks.Controllers;

public sealed class PaddleKeyboardController
{
    private World World { get; }
    private IKeyboard Keyboard { get; }
    private IPaddle Paddle => World.Paddle;

    public PaddleKeyboardController(World world, IKeyboard keyboard)
    {
        World = world;
        Keyboard = keyboard;
    }

    public void Update()
    {
        var paddle = Paddle;
        paddle.MoveLeftInput = Keyboard.IsKeyDown(KeyCode.A);
        paddle.MoveRightInput = Keyboard.IsKeyDown(KeyCode.D);
    }
}