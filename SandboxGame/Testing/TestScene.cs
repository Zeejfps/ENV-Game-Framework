using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;
using Framework.Materials;

namespace Framework;

public class TestScene : IScene
{
    private IGpuRenderbufferHandle m_TempRenderbufferHandle;
    
    private SpecularRenderPass m_SpecularRenderPass;
    private UnlitRenderPass m_UnlitRenderPass;
    private FullScreenBlitPass m_FullScreenBlitPass;
    
    private readonly IGame m_App;
    private readonly CameraRig m_CameraRig;

    private ITransform3D m_LightPosition;
    
    private Ship m_Ship1;
    private Toad m_Toad;
    private List<Ship> m_Ships;
    private readonly TestLight m_Light;
    private readonly List<ISceneObject> m_SceneObjects = new();

    private readonly IGpu m_Gpu;
    private IHandle<IGpuShader> m_UnlitShaderHandle;
    private IHandle<IGpuShader> m_FullScreenBlitShaderHandle;
    private IHandle<IGpuMesh> m_QuadMeshHandle;

    private ILogger Logger { get; }
    
    private CameraRigController CameraRigController { get; }
    
    public TestScene(IGame app, ILogger logger)
    {
        Logger = logger;

        m_App = app;
        m_Gpu = m_App.Gpu;
        var window = m_App.Window;
        var aspect = window.ScreenWidth / (float)window.ScreenHeight;
        m_CameraRig = new CameraRig(75f, aspect);
        
        m_LightPosition = new Transform3D
        {
            WorldPosition = new Vector3(0f, 5f, 0f),
        };
        m_LightPosition.RotateInLocalSpace(0f, 0f, 180f);
        
        var renderbuffer = app.Gpu.FramebufferController;
        var w = renderbuffer.WindowBufferHandle.Width;
        var h = renderbuffer.WindowBufferHandle.Height;
        m_TempRenderbufferHandle = app.Gpu.CreateRenderbuffer(3, true, w, h);

        m_SpecularRenderPass = new SpecularRenderPass(m_Gpu);

        var material = UnlitMaterial.Load(m_Gpu);
        m_UnlitRenderPass = new UnlitRenderPass(material);
        m_Light = new TestLight(m_Gpu, material, m_LightPosition);
        
        m_FullScreenBlitPass = new FullScreenBlitPass();
        
        //m_Ship1 = new Ship(m_SpecularRenderPass);

        // This also adds them to the m_SceneObjects
        // Which is bad... don't do that mmmmkkk?
        //m_Ships = CreateShips();

        m_Toad = new Toad(m_Gpu, m_SpecularRenderPass);
        
        m_SceneObjects.Add(m_Light);
        //m_SceneObjects.Add(m_Ship1);
        m_SceneObjects.Add(m_Toad);

        CameraRigController = new CameraRigController(m_CameraRig, m_App.Window, m_App.Input);
    }

    public void Load()
    {
        var gpu = m_App.Gpu;

        m_UnlitShaderHandle = gpu.ShaderController.Load("Assets/Shaders/unlit");
        m_FullScreenBlitShaderHandle = gpu.ShaderController.Load("Assets/Shaders/fullScreenQuad.glsl");
        m_QuadMeshHandle = gpu.MeshController.Load("Assets/Meshes/quad");

        m_Light.Load(this);
        m_SpecularRenderPass.Load(this);
        
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Load(this);

        var input = m_App.Input;
        input.Keyboard.KeyPressed += OnKeyPressed;
        
        CameraRigController.Enable();
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
        CameraRigController.Update(dt);
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Update(dt);
    }

    public void Render()
    {
        foreach (var sceneObject in m_SceneObjects)
            sceneObject.Render();
        
        var framebufferController = m_App.Gpu.FramebufferController;
        var windowFramebufferWidth = framebufferController.WindowBufferHandle.Width;
        var windowFramebufferHeight = framebufferController.WindowBufferHandle.Height;
        
        framebufferController.Bind(m_TempRenderbufferHandle);
        framebufferController.SetSize(windowFramebufferWidth, windowFramebufferHeight);
        framebufferController.ClearColorBuffers(0f, 0f, 0f, 0f);
        m_SpecularRenderPass.Render(m_Gpu, m_CameraRig.Camera, m_LightPosition);
        
        framebufferController.BindToWindow();
        framebufferController.ClearColorBuffers(.42f, .607f, .82f, 1f);
        m_FullScreenBlitPass.Render(m_Gpu, 
            m_CameraRig.Camera,
            m_LightPosition,
            m_QuadMeshHandle,
            m_FullScreenBlitShaderHandle,
            m_TempRenderbufferHandle.ColorBuffers[0],
            m_TempRenderbufferHandle.ColorBuffers[1],
            m_TempRenderbufferHandle.ColorBuffers[2]);
        
        m_UnlitRenderPass.Render(m_Gpu, m_CameraRig.Camera);
    }

    private List<Ship> CreateShips()
    {
        var gpu = m_App.Gpu;
        var mesh = gpu.MeshController.Load("Assets/Meshes/ship.mesh");
        var diffuse = gpu.TextureController.Load("Assets/Textures/Ship/ship_d.texture");
        var normal = gpu.TextureController.Load("Assets/Textures/Ship/ship_n.texture");
        var roughness = gpu.TextureController.Load("Assets/Textures/Ship/ship_r.texture");
        var occlusion = gpu.TextureController.Load("Assets/Textures/Ship/ship_ao.texture");
        var translucency = gpu.TextureController.Load("Assets/Textures/Toad/Toad_Translucency.texture");

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