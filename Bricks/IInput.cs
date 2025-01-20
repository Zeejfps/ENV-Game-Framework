namespace Bricks;

public interface IInput
{
    bool IsKeyDown(KeyCode keyCode);
    bool WasKeyPressedThisFrame(KeyCode keyCode);
}