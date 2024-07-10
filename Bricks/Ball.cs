using System.Numerics;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public sealed class Ball : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;

    private Vector2 m_Position;
    private Vector2 m_Velocity;
    
    public void Update(ref SpriteInstanceData instancedData)
    {
        instancedData.Tint = new Color(1f, 1f, 1f, 1f);
        instancedData.ScreenRect = new ScreenRect
        {
            X = m_Position.X,
            Y = m_Position.Y,
            Width = 20,
            Height = 20,
        };
        instancedData.AtlasRect = new ScreenRect
        {
            X = 120f,
            Y = 0f,
            Width = 20f,
            Height = 20f
        };
    }

    public Ball()
    {
        m_Position = new Vector2(320, 240);
        m_Velocity = new Vector2(200f, 200f);
    }
    
    public void Move(float dt)
    {
        m_Position += m_Velocity * dt;
        if (m_Position.X < 0)
        {
            m_Position.X = 0;
            m_Velocity.X = -m_Velocity.X;
        } 
        else if (m_Position.X > 640)
        {
            m_Position.X = 640;
            m_Velocity.X = -m_Velocity.X;
        }
        
        if (m_Position.Y < 0)
        {
            m_Velocity.Y = -m_Velocity.Y;
            m_Position.Y = 0;
        }
        else if (m_Position.Y > 480)
        {
            m_Velocity.Y = -m_Velocity.Y;
            m_Position.Y = 480;
        }
        
        
        BecameDirty?.Invoke(this);
    }
}