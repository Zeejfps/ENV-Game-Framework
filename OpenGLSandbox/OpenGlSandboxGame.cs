using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using static OpenGL.Gl;

namespace OpenGLSandbox;

public sealed class OpenGlSandboxGame : Game
{
    private readonly IScene[] m_Scenes;
    
    private int m_CurrentSceneIndex;

    public OpenGlSandboxGame(IContext context, IInputSystem inputSystem, IAssetLoader<ICpuTexture> imageLoader) : base(context)
    {
        m_Scenes = new IScene[]
        {
            new GuiEventBaseExperimentScene(Window),
            new GuiCommandBufferExperimentScene(Window, inputSystem),
            new BitmapFontRenderingScene(),
            new BasicTextureRenderingScene(imageLoader),
            new WidgetRectRenderingScene(),
            new RectNormalsRenderingScene(),
            new GouraudShadingRenderingScene(),
            new MappedBufferRenderingScene(),
            new BasicRenderingScene(),
        };

        m_CurrentSceneIndex = 0;
    }

    protected override void OnStartup()
    {
        Window.SetScreenSize(640, 640);
        Window.IsResizable = true;
        m_Scenes[m_CurrentSceneIndex].Load();
        
        Input.Keyboard.KeyPressed += Keyboard_OnKeyPressed;
    }

    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if (evt.Key != KeyboardKey.Space)
            return;
        
        var currScene = m_Scenes[m_CurrentSceneIndex];
        currScene.Unload();
            
        m_CurrentSceneIndex++;
        if (m_CurrentSceneIndex >= m_Scenes.Length)
            m_CurrentSceneIndex = 0;
            
        m_Scenes[m_CurrentSceneIndex].Load();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        m_Scenes[m_CurrentSceneIndex].Render();
    }

    protected override void OnShutdown()
    {
        m_Scenes[m_CurrentSceneIndex].Unload();
    }
}