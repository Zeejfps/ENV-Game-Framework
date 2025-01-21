namespace Bricks;

public interface IKeyboard
{
    bool IsKeyDown(KeyCode keyCode);
    bool WasKeyPressedThisFrame(KeyCode keyCode);
}