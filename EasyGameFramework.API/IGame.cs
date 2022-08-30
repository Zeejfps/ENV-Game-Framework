namespace EasyGameFramework.API;

public interface IGame
{
    bool IsRunning { get; }

    void Start();
    void Update(float dt);
    void Render(float dt);
    void Quit();
}