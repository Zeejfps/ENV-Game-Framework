using System.Numerics;
using EasyGameFramework.Api;

namespace Tetris;

[Flags]
public enum Flag
{
    None = 0,
    Gravity = 1,
    Renderable
}

public sealed class TetrisGame : Game
{
    private int m_EntityCount;
    private readonly Entity[] m_Entities = new Entity[1000];
    
    public TetrisGame(IContext context) : base(context)
    {
    }

    protected override void OnStartup()
    {
        Window.Title = "Tetris";
        Window.SetScreenSize(640, 480);
        SpawnTetromino();
    }

    private void SpawnTetromino()
    {
        ref var m1 = ref CreateEntity();
        m1.Set(Flag.Gravity);
        m1.Position = new Vector2(0, 20);

        ref var m2 = ref CreateEntity();
        m2.Set(Flag.Gravity);
        m2.Position = new Vector2(0, 19);
    }

    private ref Entity CreateEntity()
    {
        ref var entity = ref m_Entities[m_EntityCount];
        entity.Index = m_EntityCount;
        m_EntityCount++;
        return ref entity;
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

            if (entity.Has(Flag.Renderable))
            {
                
            }
        }
    }

    protected override void OnShutdown()
    {
    }
}