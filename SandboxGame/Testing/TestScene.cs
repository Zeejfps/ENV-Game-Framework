using System.Numerics;
using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.API.InputDevices;
using EasyGameFramework.Cameras;
using TicTacToePrototype;

namespace Framework;

public class TestScene : IScene
{
    public IApplication Context => m_Context;

    private IGpuRenderbuffer m_TempRenderbuffer;
    private IGpuFramebuffer m_WindowFramebuffer;
    
    private SpecularRenderPass m_SpecularRenderPass;
    private UnlitRenderPass m_UnlitRenderPass;
    private FullScreenBlitPass m_FullScreenBlitPass;
    
    private readonly IApplication m_Context;
    private readonly ICamera m_Camera;
    private readonly IClock m_Clock;

    private ITransform3D m_CameraTarget;
    private ITransform3D m_LightPosition;

    private int m_PrevMouseX;
    private int m_PrevMouseY;
    private int m_ColorBufferIndex;
    private bool m_IsRotating;

    private Ship m_Ship1;
    private Toad m_Toad;
    private List<Ship> m_Ships;
    private readonly TestLight m_Light;
    private readonly List<ISceneObject> m_SceneObjects = new();

    private IGpuShader m_UnlitShader;
    private IGpuShader m_FullScreenBlitShader;
    private IGpuMesh m_QuadMesh;
    
    public TestScene(IApplication context)
    {
        var aspect = context.Window.Width / (float)context.Window.Height;
        m_Context = context;
        //m_Camera = new OrthographicCamera(20, 20 / aspect, 0.1f, 100f);
        m_Camera = new PerspectiveCamera(75f, aspect);
        m_Clock = new Clock();
        m_Camera.Transform.WorldPosition = new Vector3(0, 5f, -25f);
        
        m_CameraTarget = new Transform3D();
        m_Camera.Transform.LookAt(m_CameraTarget.WorldPosition, Vector3.UnitY);
        
        var lightTransform = new Transform3D
        {
            WorldPosition = new Vector3(0f, 5f, 0f),
        };
        lightTransform.RotateInLocalSpace(0f, 0f, 180f);
        
        m_WindowFramebuffer = context.Window.Framebuffer;
        
        m_TempRenderbuffer = context.CreateRenderbuffer(m_WindowFramebuffer.Width, m_WindowFramebuffer.Height, 3, true);

        m_SpecularRenderPass = new SpecularRenderPass(lightTransform);
        m_UnlitRenderPass = new UnlitRenderPass();
        m_Light = new TestLight(m_UnlitRenderPass, lightTransform);
        m_FullScreenBlitPass = new FullScreenBlitPass(m_Camera,m_Light.Transform);
        
        
        //m_Ship1 = new Ship(m_SpecularRenderPass);

        // This also adds them to the m_SceneObjects
        // Which is bad... don't do that mmmmkkk?
        m_Ships = CreateShips();

        m_Toad = new Toad(m_SpecularRenderPass);
        
        m_SceneObjects.Add(m_Light);
        //m_SceneObjects.Add(m_Ship1);
        m_SceneObjects.Add(m_Toad);

        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Load(this);
    }

    public void Load()
    {
        var locator = Context.Locator;
        var meshLoader = locator.LocateOrThrow<IAssetLoader<IGpuMesh>>();
        var shaderLoader = locator.LocateOrThrow<IAssetLoader<IGpuShader>>();

        m_UnlitShader = shaderLoader.Load("Assets/Shaders/unlit.shader");

        m_UnlitShader.EnableDepthTest = true;
        m_UnlitShader.EnableBackfaceCulling = false;
        
        m_FullScreenBlitShader = shaderLoader.Load("Assets/Shaders/fullScreenQuad.shader");
        m_FullScreenBlitShader.EnableBackfaceCulling = true;
        m_FullScreenBlitShader.EnableDepthTest = false;
        
        m_QuadMesh = meshLoader.Load("Assets/Meshes/quad.mesh");

        m_Light.Load(this);
        m_SpecularRenderPass.Load(this);
        
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Load(this);
    }

    public void Unload()
    {
        
    }

    public void Update()
    {
        //m_Camera.Update();
        m_Clock.Tick();
        
        HandleInput();

        m_Light.Transform.WorldPosition += new Vector3(MathF.Sin(m_Clock.Time),0,0) * m_Clock.DeltaTime * 5;
        
        // foreach (var sceneObject in m_SceneObjects)
        //     sceneObject.Update(this);
        //
        // /*
        //  * All the Rendering steps below
        //  */
        using (var renderbuffer = m_TempRenderbuffer.Use())
        {
            renderbuffer.Resize(m_WindowFramebuffer.Width, m_WindowFramebuffer.Height);
            renderbuffer.Clear(0f, 0f, 0f, 0f);
            m_SpecularRenderPass.Render(m_Camera);
        }

        using (var renderbuffer = m_WindowFramebuffer.Use())
        {
            renderbuffer.Clear(.42f, .607f, .82f, 1f);
            m_FullScreenBlitPass.Render(m_QuadMesh,
                m_FullScreenBlitShader,
                m_TempRenderbuffer.ColorBuffers[0],
                m_TempRenderbuffer.ColorBuffers[1],
                m_TempRenderbuffer.ColorBuffers[2]);
            
            m_UnlitRenderPass.Render(m_Camera, m_UnlitShader);
        }
    }

    private void HandleInput()
    {
        var speed = m_Clock.DeltaTime * 15f;
        var mouse = m_Context.Input.Mouse;
        var keyboard = m_Context.Input.Keyboard;
        
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
        
        if (mouse.WasButtonPressedThisFrame(MouseButton.Left) 
            || mouse.WasButtonPressedThisFrame(MouseButton.Middle))
        {
            m_PrevMouseX = mouse.ScreenX;
            m_PrevMouseY = mouse.ScreenY;
        }
        
        if (mouse.IsButtonPressed(MouseButton.Left))
        {
            var deltaX = (mouse.ScreenX - m_PrevMouseX) * 0.001f;
            var deltaY = (mouse.ScreenY - m_PrevMouseY) * 0.001f;
            m_PrevMouseX = mouse.ScreenX;
            m_PrevMouseY = mouse.ScreenY;
            
            m_Camera.Transform.RotateAround(m_CameraTarget.WorldPosition, Vector3.UnitY, -deltaX);
            m_Camera.Transform.RotateAround(m_CameraTarget.WorldPosition, m_Camera.Transform.Right, -deltaY);
        }

        if (mouse.IsButtonPressed(MouseButton.Middle))
        {
            var deltaX = (mouse.ScreenX - m_PrevMouseX) * 0.001f;
            var deltaY = (mouse.ScreenY - m_PrevMouseY) * 0.001f;
            m_PrevMouseX = mouse.ScreenX;
            m_PrevMouseY = mouse.ScreenY;

            var movement = (m_Camera.Transform.Right * -deltaX + m_Camera.Transform.Up * deltaY) 
                           * m_Clock.DeltaTime * 1920f;

            m_Camera.Transform.WorldPosition += movement;
            m_CameraTarget.WorldPosition += movement;
        }
        
        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Alpha1))
            m_ColorBufferIndex = 0;
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Alpha2))
            m_ColorBufferIndex = 1;
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Alpha3))
            m_ColorBufferIndex = 2;
        
        m_Camera.Transform.LookAt(m_CameraTarget.WorldPosition, Vector3.UnitY);
    }

    private List<Ship> CreateShips()
    {
        var locator = Context.Locator;
        var meshLoader = locator.LocateOrThrow<IAssetLoader<IGpuMesh>>();
        var textureLoader = locator.LocateOrThrow<IAssetLoader<IGpuTexture>>();
        
        var mesh = meshLoader.Load("Assets/Meshes/ship.mesh");
        var diffuse = textureLoader.Load("Assets/Textures/Ship/ship_d.texture");
        var normal = textureLoader.Load("Assets/Textures/Ship/ship_n.texture");
        var roughness = textureLoader.Load("Assets/Textures/Ship/ship_r.texture");
        var occlusion = textureLoader.Load("Assets/Textures/Ship/ship_ao.texture");
        var translucency = textureLoader.Load("Assets/Textures/Toad/Toad_Translucency.texture");

        var ships = new List<Ship>();
        var size = 10;
        var count = 10;
        for (var cols = 0; cols < count; cols++)
        {
            for (var rows = 0; rows < count; rows++)
            {
                var ship = new Ship(m_SpecularRenderPass, mesh, diffuse, normal, roughness, occlusion, translucency)
                {
                    Transform =
                    {
                        WorldPosition = new Vector3(rows * size - size * count/2, cols * size - size * count/2, 0f)
                    }
                };
                ships.Add(ship);
                m_SceneObjects.Add(ship);
            }
        }

        return ships;
    }
}