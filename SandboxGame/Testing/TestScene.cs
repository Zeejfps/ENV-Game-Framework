using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;
using Framework.Materials;

namespace Framework;

public class TestScene : IScene
{
    public IContext Context => m_App;

    private IGpuRenderbufferHandle m_TempRenderbufferHandle;
    
    private SpecularRenderPass m_SpecularRenderPass;
    private UnlitRenderPass m_UnlitRenderPass;
    private FullScreenBlitPass m_FullScreenBlitPass;
    
    private readonly IContext m_App;
    private readonly ICamera m_Camera;
    private readonly Clock m_Clock;

    private ITransform3D m_CameraTarget;
    private ITransform3D m_LightPosition;
    
    private Ship m_Ship1;
    private Toad m_Toad;
    private List<Ship> m_Ships;
    private readonly TestLight m_Light;
    private readonly List<ISceneObject> m_SceneObjects = new();

    private IGpu m_Gpu;
    private IHandle<IGpuShader> m_UnlitShaderHandle;
    private IHandle<IGpuShader> m_FullScreenBlitShaderHandle;
    private IHandle<IGpuMesh> m_QuadMeshHandle;

    private ILogger Logger { get; }
    
    private CameraController CameraController { get; }
    
    public TestScene(IContext app, ILogger logger)
    {
        Logger = logger;
        
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
        
        var renderbuffer = app.Gpu.Renderbuffer;
        m_TempRenderbufferHandle = renderbuffer.CreateRenderbuffer(3, true);

        m_SpecularRenderPass = new SpecularRenderPass();

        var material = UnlitMaterial.Load(m_Gpu);
        m_UnlitRenderPass = new UnlitRenderPass(material);
        m_Light = new TestLight(material, m_LightPosition);
        
        m_FullScreenBlitPass = new FullScreenBlitPass();
        
        //m_Ship1 = new Ship(m_SpecularRenderPass);

        // This also adds them to the m_SceneObjects
        // Which is bad... don't do that mmmmkkk?
        m_Ships = CreateShips();

        m_Toad = new Toad(m_SpecularRenderPass);
        
        m_SceneObjects.Add(m_Light);
        //m_SceneObjects.Add(m_Ship1);
        m_SceneObjects.Add(m_Toad);

        CameraController = new CameraController(m_Camera, m_CameraTarget, m_App.Window, m_App.Input);
    }

    public void Load()
    {
        var gpu = Context.Gpu;

        m_UnlitShaderHandle = gpu.Shader.Load("Assets/Shaders/unlit");
        m_FullScreenBlitShaderHandle = gpu.Shader.Load("Assets/Shaders/fullScreenQuad");
        m_QuadMeshHandle = gpu.Mesh.Load("Assets/Meshes/quad");

        m_Light.Load(this);
        m_SpecularRenderPass.Load(this);
        
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Load(this);

        var input = m_App.Input;
        input.Keyboard.KeyPressed += OnKeyPressed;
        
        CameraController.Enable();
    }

    private void OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        switch (evt.Key)
        {
            case KeyboardKey.Escape when m_App.Window.IsFullscreen:
                m_App.Window.IsFullscreen = false;
                break;
        }
    }

    public void Update(float dt)
    {
        m_Clock.Tick(dt);

        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Update(m_Clock.DeltaTime);

        CameraController.Update(dt);
    }

    public void Render()
    {
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Render();
        
        var renderbuffer = Context.Gpu.Renderbuffer;
        var windowFramebufferWidth = renderbuffer.WindowBufferHandle.Width;
        var windowFramebufferHeight = renderbuffer.WindowBufferHandle.Height;
        
        renderbuffer.Bind(m_TempRenderbufferHandle);
        renderbuffer.SetSize(windowFramebufferWidth, windowFramebufferHeight);
        renderbuffer.ClearColorBuffers(0f, 0f, 0f, 0f);
        m_SpecularRenderPass.Render(m_Gpu, m_Camera, m_LightPosition);
        
        renderbuffer.BindWindow();
        renderbuffer.ClearColorBuffers(.42f, .607f, .82f, 1f);
        m_FullScreenBlitPass.Render(m_Gpu, 
            m_Camera,
            m_LightPosition,
            m_QuadMeshHandle,
            m_FullScreenBlitShaderHandle,
            m_TempRenderbufferHandle.ColorBuffers[0],
            m_TempRenderbufferHandle.ColorBuffers[1],
            m_TempRenderbufferHandle.ColorBuffers[2]);
        
        m_UnlitRenderPass.Render(m_Gpu, m_Camera);
    }

    private List<Ship> CreateShips()
    {
        var gpu = Context.Gpu;
        var mesh = gpu.Mesh.Load("Assets/Meshes/ship.mesh");
        var diffuse = gpu.Texture.Load("Assets/Textures/Ship/ship_d.texture");
        var normal = gpu.Texture.Load("Assets/Textures/Ship/ship_n.texture");
        var roughness = gpu.Texture.Load("Assets/Textures/Ship/ship_r.texture");
        var occlusion = gpu.Texture.Load("Assets/Textures/Ship/ship_ao.texture");
        var translucency = gpu.Texture.Load("Assets/Textures/Toad/Toad_Translucency.texture");

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