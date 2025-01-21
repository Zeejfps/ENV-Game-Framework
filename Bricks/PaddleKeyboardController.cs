using Bricks.Archetypes;

namespace Bricks;

public sealed class PaddleKeyboardController
{
    private Game Game { get; }
    private IKeyboard Keyboard { get; }
    private IPaddle Paddle => Game.Paddle;

    public PaddleKeyboardController(IKeyboard keyboard, Game game)
    {
        Game = game;
        Keyboard = keyboard;
    }

    public void Update()
    {
        var paddle = Paddle;
        paddle.MoveLeftInput = Keyboard.IsKeyDown(KeyCode.A);
        paddle.MoveRightInput = Keyboard.IsKeyDown(KeyCode.D);
    }
}