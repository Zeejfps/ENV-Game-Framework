using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework;

public class SpecularRenderPass
{
    private readonly Dictionary<(IHandle<IGpuMesh>, SpecularRenderableTextures), List<ITransform3D>> m_MeshToRenderableMap = new();
    private readonly Dictionary<ITransform3D, (IHandle<IGpuMesh>, SpecularRenderableTextures)> m_TransformToGroupMap = new();

    private IHandle<IGpuShader> m_SpecularShaderHandle;
    private readonly ITransform3D m_Light;

    private Vector3 _lightColor = new Vector3(1f,1f,1f);
    private Vector3 _ambientColor = new Vector3(.2f,.4f,.6f);
    private Vector3 _specularColor = new Vector3(.7f,.7f,.7f);
    private float _shininess = 10f;
    
    public SpecularRenderPass(ITransform3D light)
    {
        m_Light = light;
    }

    public void Register(in SpecularRenderable renderable)
    {
        var mesh = renderable.MeshHandle;
        var textures = renderable.Textures;
        var transform = renderable.Transform;
        
        var key = (mesh, textures);
        if (!m_MeshToRenderableMap.TryGetValue(key, out var transforms))
        {
            transforms = new List<ITransform3D>();
            m_MeshToRenderableMap[key] = transforms;
        }
        
        transforms.Add(transform);
        m_TransformToGroupMap[transform] = key;
    }
    
    public void Load(IScene scene)
    {
        m_SpecularShaderHandle = scene.App.Gpu.LoadShader("Assets/Shaders/specular.shader");
    }
    
    public void Render(IGpu gpu, ICamera camera)
    {
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        gpu.SaveState();
        gpu.EnableBackfaceCulling = true;
        gpu.EnableDepthTest = true;
        
        using var shader = m_SpecularShaderHandle.Use();
        shader.SetVector3("light.position", m_Light.WorldPosition);
        shader.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shader.SetMatrix4x4("matrix_view", viewMatrix);
        shader.SetVector3("camera_position", camera.Transform.WorldPosition);
        shader.SetVector3("light.diffuse", _lightColor);
        shader.SetVector3("light.specular", _specularColor);
        shader.SetVector3("light.ambient", _ambientColor);
        shader.SetFloat("material.shininess", _shininess);

        var modelMatricesBuffer = shader.GetBuffer("model_matrices_t");
        
        foreach (var renderGroup in m_MeshToRenderableMap.Keys)
        {
            using var mesh = renderGroup.Item1.Use();
            var textures = renderGroup.Item2;
            
            shader.SetTexture2d("material.diffuse", textures.Diffuse);
            shader.SetTexture2d("material.normal_map", textures.Normal);
            shader.SetTexture2d("material.roughness_map", textures.Roughness);
            shader.SetTexture2d("material.occlusion", textures.Occlusion);
            shader.SetTexture2d("material.translucency", textures.Translucency);
            
            var transforms = m_MeshToRenderableMap[renderGroup];
            var transformsCount = transforms.Count;

            using (var buffer = modelMatricesBuffer.Use())
            {
                buffer.Clear();

                for (var i = 0; i < transformsCount; i++)
                {
                    var transform = transforms[i];
                    buffer.Put(transform.WorldMatrix);
                }
                
                buffer.Apply();
            }

            mesh.RenderInstanced(transforms.Count);
        }

        gpu.RestoreState();
    }
}

public readonly struct SpecularRenderable 
{
    public IHandle<IGpuMesh> MeshHandle { get; init; }
    public ITransform3D Transform { get; init; }
    public SpecularRenderableTextures Textures { get; init; }
}

public struct SpecularRenderableTextures : IEquatable<SpecularRenderableTextures>
{
    public IHandle<IGpuTexture> Diffuse { get; init; }
    public IHandle<IGpuTexture> Normal { get; init; }
    public IHandle<IGpuTexture> Roughness { get; init; }
    public IHandle<IGpuTexture> Occlusion { get; init; }
    public IHandle<IGpuTexture> Translucency { get; init; }

    public bool Equals(SpecularRenderableTextures other)
    {
        return Diffuse.Equals(other.Diffuse) &&
               Normal.Equals(other.Normal) &&
               Roughness.Equals(other.Roughness) &&
               Occlusion.Equals(other.Occlusion) &&
               Translucency.Equals(other.Translucency);
    }

    public override bool Equals(object? obj)
    {
        return obj is SpecularRenderableTextures other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Diffuse, Normal, Roughness, Occlusion, Translucency);
    }

    public static bool operator ==(SpecularRenderableTextures left, SpecularRenderableTextures right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SpecularRenderableTextures left, SpecularRenderableTextures right)
    {
        return !left.Equals(right);
    }
}