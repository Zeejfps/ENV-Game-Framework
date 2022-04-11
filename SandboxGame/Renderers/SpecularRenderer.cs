using System.Diagnostics;
using System.Numerics;

namespace Framework;

public readonly struct SpecularRendererData
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
    private IMaterial? m_Material;
    
    private IFramebuffer? m_Framebuffer;
    
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
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Material = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/specular.material");
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

    public void Render(SpecularRendererData data)
    {
        var camera = m_Camera;
        var modelMatrix = data.Transform.WorldMatrix;
        var framebuffer = m_Framebuffer;
        var mesh = data.Mesh;
        var material = m_Material;

        Matrix4x4.Invert(modelMatrix, out var normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);
        
        Debug.Assert(material != null);
        Debug.Assert(framebuffer != null);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);

        material.Use();
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
        material.SetTexture2d("material.diffuse", data.Diffuse);
        material.SetTexture2d("material.normal_map", data.Normal);
        material.SetTexture2d("material.roughness_map", data.Roughness);
        material.SetTexture2d("material.occlusion", data.Occlusion);
        material.SetTexture2d("material.translucency", data.Translucency);
        mesh.Render();
    }
}