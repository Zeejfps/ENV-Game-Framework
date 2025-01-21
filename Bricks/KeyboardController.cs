namespace Bricks;

public sealed class KeyboardController
{
    private Game Game { get; }
    private IKeyboard Keyboard { get; }

    public KeyboardController(IKeyboard keyboard, Game game)
    {
        Game = game;
        Keyboard = keyboard;
    }

    public void Update()
    {
        var paddle = Game.Paddle;
        paddle.MoveLeftInput = Keyboard.IsKeyDown(KeyCode.A);
        paddle.MoveRightInput = Keyboard.IsKeyDown(KeyCode.D);
    }
}