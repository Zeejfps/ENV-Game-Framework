using System.Numerics;
using EasyGameFramework.Api;

namespace Tetris;

[Flags]
public enum Flag
{
    None = 0,
    Gravity = 1,
}

public sealed class TetrisGame : Game
{
    private readonly Entity[] m_Entities = new Entity[1000];
    
    public TetrisGame(IContext context) : base(context)
    {
    }

    protected override void OnStartup()
    {
        Window.Title = "Tetris";
        Window.SetScreenSize(640, 480);
        Window.OpenCentered();

        var monomino = m_Entities[0];
        monomino.Set(Flag.Gravity);
        monomino.Position = new Vector2();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        for (var i = 0; i < m_Entities.Length; i++)
        {
            ref var entity = ref m_Entities[i];
            if (entity.Has(Flag.Gravity))
            {
                entity.Position -= Vector2.UnitY;

                if (entity.Position.Y <= 0)
                {
                    entity.Clear(Flag.Gravity);   
                }
            }
        }
    }

    protected override void OnShutdown()
    {
    }
}