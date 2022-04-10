using System.Diagnostics;
using System.Numerics;
using Framework;

namespace Framework;

public struct SpecularRendererData
{
    public IMesh Mesh { get; init; }
    public ITransform Transform { get; init; }
}

public class SpecularRenderer : ISceneObject
{
    private IMaterial? m_Material;
    private ITexture? m_Texture;
    private IFramebuffer? m_Framebuffer;
    
    private readonly ICamera m_Camera;
    private readonly ITransform m_Light;

    private Vector3 _lightColor = new Vector3(1f,1f,1f);
    private Vector3 _ambientColor = new Vector3(.1f,.1f,.2f);
    private Vector3 _specularColor = new Vector3(1f,1f,1f);
    private float _shininess = 16f;

    public SpecularRenderer(ICamera camera, ITransform light)
    {
        m_Camera = camera;
        m_Light = light;
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Material = assetDatabase.LoadAsset<IMaterial>("Assets/Shaders/specular.json");
        m_Texture = assetDatabase.LoadAsset<ITexture>("Assets/Textures/uvgrid.texture");
        m_Framebuffer = scene.Context.Window.Framebuffer;
    }

    public void Update(IScene scene)
    {
        
    }

    public void Unload(IScene scene)
    {
        Debug.Assert(m_Material != null);
        m_Material.Unload();
        m_Material = null;
    }

    public void Render(SpecularRendererData renderData)
    {
        var camera = m_Camera;
        var modelMatrix = renderData.Transform.WorldMatrix;
        var framebuffer = m_Framebuffer;
        var mesh = renderData.Mesh;
        var material = m_Material;

        Matrix4x4.Invert(modelMatrix, out var normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);
        
        Debug.Assert(material != null);
        Debug.Assert(framebuffer != null);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        material.SetVector3("light.position", m_Light.WorldPosition);
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", viewMatrix);
        material.SetMatrix4x4("matrix_model", modelMatrix);
        material.SetMatrix4x4("normal_matrix", normalMatrix);
        material.SetVector3("camera_position", camera.Transform.WorldPosition);
        material.SetVector3("light.diffuse", _lightColor);
        material.SetVector3("light.specular", _specularColor);
        material.SetVector3("light.ambient", _ambientColor);
        material.SetFloat("material.shininess", _shininess);


        framebuffer.RenderMesh(mesh, material);
    }
}