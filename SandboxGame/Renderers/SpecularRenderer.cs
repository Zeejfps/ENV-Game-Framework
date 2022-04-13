using System.Diagnostics;
using System.Numerics;
using Framework.InputDevices;

namespace Framework;

public interface ISpecularRenderable
{
    public IMesh Mesh { get; }
    public ITransform Transform { get; }
    public ITexture Diffuse { get; }
    public ITexture Normal { get; }
    public ITexture Roughness { get; }
    public ITexture Occlusion { get; }
    public ITexture Translucency { get; }
}

public class SpecularRenderable : ISpecularRenderable
{
    public IMesh Mesh { get; init; }
    public ITransform Transform { get; init; }
    public ITexture Diffuse { get; init; }
    public ITexture Normal { get; init; }
    public ITexture Roughness { get; init; }
    public ITexture Occlusion { get; init; }
    public ITexture Translucency { get; init; }
}

public class SpecularRenderer : ISceneObject
{
    private readonly Dictionary<IMesh, List<ISpecularRenderable>> m_MeshToRenderableMap = new();

    private IMaterial? m_Material;
    private IMaterial? m_FullScreenBlitMaterial;

    private IFramebuffer? m_WindowFramebuffer;
    private IRenderbuffer? m_TestRenderbuffer;

    private IMesh m_QuadMesh;
    
    private readonly ICamera m_Camera;
    private readonly ITransform m_Light;

    private Vector3 _lightColor = new Vector3(1f,1f,1f);
    private Vector3 _ambientColor = new Vector3(.2f,.4f,.6f);
    private Vector3 _specularColor = new Vector3(.7f,.7f,.7f);
    private float _shininess = 10f;
    
    public SpecularRenderer(ICamera camera, ITransform light)
    {
        m_Camera = camera;
        m_Light = light;
    }

    public void Add(ISpecularRenderable renderable)
    {
        var mesh = renderable.Mesh;
        if (!m_MeshToRenderableMap.TryGetValue(mesh, out var renderables))
        {
            renderables = new List<ISpecularRenderable>();
            m_MeshToRenderableMap[mesh] = renderables;
        }
        
        renderables.Add(renderable);
    }

    public void Remove(ISpecularRenderable renderable)
    {
        
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Material = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/specular.material");
        m_Material.UseBackfaceCulling = true;
        m_Material.UseDepthTest = true;
        
        m_FullScreenBlitMaterial = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/fullScreenQuad.material");
        m_FullScreenBlitMaterial.UseBackfaceCulling = true;
        m_FullScreenBlitMaterial.UseDepthTest = false;
        
        m_QuadMesh = assetDatabase.LoadAsset<IMesh>("Assets/Meshes/quad.mesh");
        //m_QuadMesh = assetDatabase.LoadAsset<IMesh>("Assets/Meshes/Toad.mesh");
        m_WindowFramebuffer = scene.Context.Window.Framebuffer;
        m_TestRenderbuffer = scene.Context.CreateRenderbuffer(m_WindowFramebuffer.Width, m_WindowFramebuffer.Height, 3, true);
    }

    private int m_ColorBufferIndex;
    
    public void Update(IScene scene)
    {
        var keyboard = scene.Context.Window.Input.Keyboard;
        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Alpha1))
            m_ColorBufferIndex = 0;
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Alpha2))
            m_ColorBufferIndex = 1;
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Alpha3))
            m_ColorBufferIndex = 2;
        
        RenderOpaquePass();
        RenderFullScreenQuadPass();
    }

    public void Unload(IScene scene)
    {
        Debug.Assert(m_Material != null);
        m_Material.Unload();
        m_Material = null;
    }

    private void RenderFullScreenQuadPass()
    {
        Debug.Assert(m_TestRenderbuffer != null);

        Debug.Assert(m_WindowFramebuffer != null);
        using var framebuffer = m_WindowFramebuffer.Use();
        framebuffer.Clear(.42f, .607f, .82f);
        
        Debug.Assert(m_FullScreenBlitMaterial != null);
        using var material = m_FullScreenBlitMaterial.Use();
        material.SetTexture2d("screenTexture", m_TestRenderbuffer.ColorBuffers[m_ColorBufferIndex]);

        m_QuadMesh.Render();
    }

    private void RenderOpaquePass()
    {
        Debug.Assert(m_TestRenderbuffer != null);

        using var renderBuffer = m_TestRenderbuffer.Use();
        renderBuffer.Resize(m_WindowFramebuffer.Width, m_WindowFramebuffer.Height);
        renderBuffer.Clear(.42f, .607f, .82f);

        var camera = m_Camera;
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);

        Debug.Assert(m_Material != null);
        
        using var material = m_Material.Use();
        material.SetVector3("light.position", m_Light.WorldPosition);
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", viewMatrix);
        material.SetVector3("camera_position", camera.Transform.WorldPosition);
        material.SetVector3("light.diffuse", _lightColor);
        material.SetVector3("light.specular", _specularColor);
        material.SetVector3("light.ambient", _ambientColor);
        material.SetFloat("material.shininess", _shininess);

        foreach (var kvp in m_MeshToRenderableMap)
        {
            var mesh = kvp.Key;
            foreach (var renderable in kvp.Value)
            {
                var modelMatrix = renderable.Transform.WorldMatrix;
                Matrix4x4.Invert(modelMatrix, out var normalMatrix);
                normalMatrix = Matrix4x4.Transpose(normalMatrix);
                
                material.SetMatrix4x4("matrix_model", modelMatrix);
                material.SetMatrix4x4("normal_matrix", normalMatrix);
                material.SetTexture2d("material.diffuse", renderable.Diffuse);
                material.SetTexture2d("material.normal_map", renderable.Normal);
                material.SetTexture2d("material.roughness_map", renderable.Roughness);
                material.SetTexture2d("material.occlusion", renderable.Occlusion);
                material.SetTexture2d("material.translucency", renderable.Translucency);
                
                mesh.Render();
            }
        }
    }
}