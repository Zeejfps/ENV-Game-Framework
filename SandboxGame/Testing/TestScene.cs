using System.Numerics;
using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.API.InputDevices;
using EasyGameFramework.Cameras;

namespace Framework;

public class TestScene : IScene
{
    public IApplication App => m_App;

    private IGpuRenderbufferHandle m_TempRenderbufferHandle;
    
    private SpecularRenderPass m_SpecularRenderPass;
    private UnlitRenderPass m_UnlitRenderPass;
    private FullScreenBlitPass m_FullScreenBlitPass;
    
    private readonly IApplication m_App;
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

    private IGpu m_Gpu;
    private IHandle<IGpuShader> m_UnlitShaderHandle;
    private IHandle<IGpuShader> m_FullScreenBlitShaderHandle;
    private IHandle<IGpuMesh> m_QuadMeshHandle;

    public TestScene(IApplication app)
    {
        var aspect = app.Window.Width / (float)app.Window.Height;
        m_App = app;
        m_Gpu = m_App.Gpu;
        //m_Camera = new OrthographicCamera(20, 20 / aspect, 0.1f, 100f);
        m_Camera = new PerspectiveCamera(75f, aspect);
        m_Clock = new Clock();
        m_Camera.Transform.WorldPosition = new Vector3(0, 5f, -25f);
        
        m_CameraTarget = new Transform3D();
        m_Camera.Transform.LookAt(m_CameraTarget.WorldPosition, Vector3.UnitY);
        
        m_LightPosition = new Transform3D
        {
            WorldPosition = new Vector3(0f, 5f, 0f),
        };
        m_LightPosition.RotateInLocalSpace(0f, 0f, 180f);
        
        var framebufferManager = app.Gpu.RenderbufferManager;
        var framebufferWidth = framebufferManager.WindowBufferHandle.Width;
        var framebufferHeight = framebufferManager.WindowBufferHandle.Height;
        m_TempRenderbufferHandle = app.Gpu.CreateRenderbuffer(framebufferWidth, framebufferHeight, 3, true);

        m_SpecularRenderPass = new SpecularRenderPass();
        m_UnlitRenderPass = new UnlitRenderPass();
        m_Light = new TestLight(m_UnlitRenderPass, m_LightPosition);
        m_FullScreenBlitPass = new FullScreenBlitPass();
        
        //m_Ship1 = new Ship(m_SpecularRenderPass);

        // This also adds them to the m_SceneObjects
        // Which is bad... don't do that mmmmkkk?
        m_Ships = CreateShips();

        m_Toad = new Toad(m_SpecularRenderPass);
        
        m_SceneObjects.Add(m_Light);
        //m_SceneObjects.Add(m_Ship1);
        m_SceneObjects.Add(m_Toad);
    }

    public void Load()
    {
        var gpu = App.Gpu;

        m_UnlitShaderHandle = gpu.LoadShader("Assets/Shaders/unlit.shader");
        m_FullScreenBlitShaderHandle = gpu.LoadShader("Assets/Shaders/fullScreenQuad.shader");
        m_QuadMeshHandle = gpu.LoadMesh("Assets/Meshes/quad.mesh");

        m_Light.Load(this);
        m_SpecularRenderPass.Load(this);
        
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Load(this);
    }

    public void Update()
    {
        //m_Camera.Update();
        m_Clock.Tick();
        
        HandleInput();

        m_Light.Transform.WorldPosition += new Vector3(MathF.Sin(m_Clock.Time),0,0) * m_Clock.DeltaTime * 5;
        
        // foreach (var sceneObject in m_SceneObjects)
        //     sceneObject.Update(this);
        
        // /*
        //  * All the Rendering steps below
        //  */
        
        var renderbufferManager = App.Gpu.RenderbufferManager;
        var windowFramebufferWidth = renderbufferManager.WindowBufferHandle.Width;
        var windowFramebufferHeight = renderbufferManager.WindowBufferHandle.Height;
        
        renderbufferManager.Bind(m_TempRenderbufferHandle);
        renderbufferManager.SetSize(windowFramebufferWidth, windowFramebufferHeight);
        renderbufferManager.ClearColorBuffer(0f, 0f, 0f, 0f);
        m_SpecularRenderPass.Render(m_Gpu, m_Camera, m_LightPosition);
        
        renderbufferManager.BindWindow();
        renderbufferManager.ClearColorBuffer(.42f, .607f, .82f, 1f);
        m_FullScreenBlitPass.Render(m_Gpu, 
            m_Camera,
            m_LightPosition,
            m_QuadMeshHandle,
            m_FullScreenBlitShaderHandle,
            m_TempRenderbufferHandle.ColorBuffers[0],
            m_TempRenderbufferHandle.ColorBuffers[1],
            m_TempRenderbufferHandle.ColorBuffers[2]);
        
        m_UnlitRenderPass.Render(m_Gpu, m_UnlitShaderHandle, m_Camera);
    }

    private void HandleInput()
    {
        var speed = m_Clock.DeltaTime * 15f;
        var mouse = m_App.Input.Mouse;
        var keyboard = m_App.Input.Keyboard;
        
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
        
        if (m_App.Window.IsFullscreen && keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
            m_App.Window.IsFullscreen = false;

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
        var gpu = App.Gpu;
        var mesh = gpu.LoadMesh("Assets/Meshes/ship.mesh");
        var diffuse = gpu.LoadTexture("Assets/Textures/Ship/ship_d.texture");
        var normal = gpu.LoadTexture("Assets/Textures/Ship/ship_n.texture");
        var roughness = gpu.LoadTexture("Assets/Textures/Ship/ship_r.texture");
        var occlusion = gpu.LoadTexture("Assets/Textures/Ship/ship_ao.texture");
        var translucency = gpu.LoadTexture("Assets/Textures/Toad/Toad_Translucency.texture");

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