using System.Diagnostics;
using EasyGameFramework.API;

namespace EasyGameFramework;

public class Engine : IEngine
{
    private IGame Game { get; }
    private IInput Input { get; }
    private IWindow Window { get; }

    private readonly Stopwatch m_Stopwatch;

    public Engine(IInput input, IWindow window, IGame game)
    {
        Game = game;
        Input = input;
        Window = window;
        m_Stopwatch = new Stopwatch();
    }

    public void Run()
    {
        var game = Game;
        var input = Input;
        var window = Window;
        
        game.Start();
        m_Stopwatch.Start();
        while (game.IsRunning && window.IsOpened)
        {
            var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
            var deltaTime = (float)deltaTimeTicks / Stopwatch.Frequency;
            m_Stopwatch.Restart();

            input.Update();
            window.Update();
            game.Update(deltaTime);
            game.Render(0f);
        }
    }
}