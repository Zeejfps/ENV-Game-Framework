using System.Diagnostics;
using System.Numerics;
using Framework;
using Framework.InputDevices;
using TicTacToePrototype;

namespace Framework;

public class TestScene : IScene
{
    public IContext Context => m_Context;

    private SpecularRenderer m_SpecularRenderer;
    private UnlitRenderer m_UnlitRenderer;
    
    private Ship m_Ship1;
    private Ship m_Ship2;
    private Toad m_Toad;

    private List<Ship> m_Ships;

    private readonly IContext m_Context;
    private readonly ICamera m_Camera;
    private readonly IClock m_Clock;

    private readonly TestLight m_Light;

    private readonly List<ISceneObject> m_SceneObjects = new();
    
    public TestScene(IContext context)
    {
        m_Context = context;
        m_Camera = new PerspectiveCamera();
        m_Clock = new TestClock();
        m_Camera.Transform.WorldPosition = new Vector3(0, 5f, -25f);
        m_Camera.Transform.LookAt(Vector3.UnitY, Vector3.UnitY);
        
        var lightTransform = new Transform3D
        {
            WorldPosition = new Vector3(0f, 5f, 0f),
        };

        m_SpecularRenderer = new SpecularRenderer(m_Camera, lightTransform);
        m_UnlitRenderer = new UnlitRenderer(m_Camera);
        m_Light = new TestLight(m_UnlitRenderer, lightTransform);

        m_Ship1 = new Ship(m_SpecularRenderer);
        m_Ship2 = new Ship(m_SpecularRenderer);
        m_Ship2.Transform.WorldPosition = new Vector3(10f, 0f, 0f);

        m_Ships = new List<Ship>();
        var size = 10;
        var count = 10;
        for (var cols = 0; cols < count; cols++)
        {
            for (var rows = 0; rows < count; rows++)
            {
                var ship = new Ship(m_SpecularRenderer)
                {
                    Transform =
                    {
                        WorldPosition = new Vector3(rows * size - size * count/2, cols * size - size * count/2, 0f)
                    }
                };
                m_Ships.Add(ship);
                m_SceneObjects.Add(ship);
            }
        }
        
        m_Toad = new Toad(m_SpecularRenderer);
        
        m_SceneObjects.Add(m_UnlitRenderer);
        m_SceneObjects.Add(m_Camera);
        m_SceneObjects.Add(m_Light);
        m_SceneObjects.Add(m_Clock);
        m_SceneObjects.Add(m_Ship1);
        m_SceneObjects.Add(m_Ship2);
        m_SceneObjects.Add(m_Toad);
    }

    public void Load()
    {
        m_SpecularRenderer.Load(this);
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Load(this);
    }

    public void Unload()
    {
        
    }

    private int m_PrevMouseX;
    private int m_PrevMouseY;
    private bool m_IsRotating;
    
    public void Update()
    {
        var speed = m_Clock.DeltaTime * 15f;
        var mouse = m_Context.Window.Input.Mouse;
        var keyboard = m_Context.Window.Input.Keyboard;
        
        if (keyboard.IsKeyPressed(KeyboardKey.W))
            m_Camera.Transform.WorldPosition += m_Camera.Transform.Forward * speed;
        else if (keyboard.IsKeyPressed(KeyboardKey.S))
            m_Camera.Transform.WorldPosition -= m_Camera.Transform.Forward * speed;
        
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            m_Camera.Transform.WorldPosition -= m_Camera.Transform.Right * speed;
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            m_Camera.Transform.WorldPosition += m_Camera.Transform.Right * speed;
        
        if (mouse.ScrollDeltaY != 0)
            m_Camera.Transform.WorldPosition += m_Camera.Transform.Forward * mouse.ScrollDeltaY * m_Clock.DeltaTime * 100f;
        
        if (m_Context.Window.IsFullscreen && keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
            m_Context.Window.IsFullscreen = false;

        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Space))
            m_IsRotating = !m_IsRotating;
        
        if (mouse.WasButtonPressedThisFrame(MouseButton.Left))
        {
            m_PrevMouseX = mouse.ScreenX;
            m_PrevMouseY = mouse.ScreenY;
        }
        
        if (mouse.IsButtonPressed(MouseButton.Left))
        {
            var deltaX = (mouse.ScreenX - m_PrevMouseX) * m_Clock.DeltaTime * 1f;
            var deltaY = (mouse.ScreenY - m_PrevMouseY) * m_Clock.DeltaTime * 1f;
            m_PrevMouseX = mouse.ScreenX;
            m_PrevMouseY = mouse.ScreenY;
            
            m_Camera.Transform.RotateAround(Vector3.Zero, Vector3.UnitY, -deltaX);
            m_Camera.Transform.RotateAround(Vector3.Zero, m_Camera.Transform.Right, -deltaY);
        }
        
        m_Camera.Transform.LookAt(Vector3.UnitY, Vector3.UnitY);
        
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Update(this);
        
        m_SpecularRenderer.Update(this);
    }
}

public interface IClock : ISceneObject
{
    float DeltaTime { get; }
}

public class TestClock : IClock
{
    public float DeltaTime { get; private set; }

    private long m_PrevTime;

    public TestClock()
    {
        m_PrevTime = Stopwatch.GetTimestamp();
    }

    public void Load(IScene scene)
    {
        
    }

    public void Update(IScene scene)
    {
        var currTime = Stopwatch.GetTimestamp();
        var deltaTime = currTime - m_PrevTime;
        m_PrevTime = currTime;

        DeltaTime = (float)deltaTime / Stopwatch.Frequency;
    }

    public void Unload(IScene scene)
    {
        
    }
}