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

public class SpecularRenderPass
{
    private readonly Dictionary<IMesh, List<ISpecularRenderable>> m_MeshToRenderableMap = new();

    private IMaterial? m_SpecularMaterial;
    private readonly ITransform m_Light;

    private Vector3 _lightColor = new Vector3(1f,1f,1f);
    private Vector3 _ambientColor = new Vector3(.2f,.4f,.6f);
    private Vector3 _specularColor = new Vector3(.7f,.7f,.7f);
    private float _shininess = 10f;
    
    public SpecularRenderPass(ITransform light)
    {
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
        m_SpecularMaterial = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/specular.material");
        m_SpecularMaterial.UseBackfaceCulling = true;
        m_SpecularMaterial.UseDepthTest = true;
    }
    
    public void Render(ICamera camera)
    {
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);

        Debug.Assert(m_SpecularMaterial != null);
        
        using var material = m_SpecularMaterial.Use();
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
                
                // TODO: This can be optimized, no point setting the textures if they are the same
                material.SetTexture2d("material.diffuse", renderable.Diffuse);
                material.SetTexture2d("material.normal_map", renderable.Normal);
                material.SetTexture2d("material.roughness_map", renderable.Roughness);
                material.SetTexture2d("material.occlusion", renderable.Occlusion);
                material.SetTexture2d("material.translucency", renderable.Translucency);
                
                mesh.Render();
            }
        }
    }

    public void Unload(IScene scene)
    {
        Debug.Assert(m_SpecularMaterial != null);
        m_SpecularMaterial.Unload();
        m_SpecularMaterial = null;
    }
}