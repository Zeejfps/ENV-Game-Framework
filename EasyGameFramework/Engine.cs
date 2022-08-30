using EasyGameFramework.API;

namespace EasyGameFramework;

public class Engine : IEngine
{
    private IGame Game { get; }
    private IInput Input { get; }
    private IWindow Window { get; }

    public Engine(IInput input, IWindow window, IGame game)
    {
        Game = game;
        Input = input;
        Window = window;
    }

    public void Run()
    {
        var game = Game;
        var input = Input;
        var window = Window;
        
        game.Start();
        while (game.IsRunning && window.IsOpened)
        {
            input.Update();
            window.Update();
            game.Update(0f);
        }
    }
}