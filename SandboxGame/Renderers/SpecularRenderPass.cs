using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Framework;

public class SpecularRenderPass
{
    private readonly Dictionary<(IHandle<IGpuMesh>, SpecularRenderableTextures), List<ITransform3D>> m_MeshToRenderableMap = new();
    private readonly Dictionary<ITransform3D, (IHandle<IGpuMesh>, SpecularRenderableTextures)> m_TransformToGroupMap = new();

    private IHandle<IGpuShader> m_SpecularShaderHandle;

    private Vector3 _lightColor = new Vector3(1f,1f,1f);
    private Vector3 _ambientColor = new Vector3(.2f,.4f,.6f);
    private Vector3 _specularColor = new Vector3(.7f,.7f,.7f);
    private float _shininess = 10f;

    private IGpu Gpu { get; }

    public SpecularRenderPass(IGpu gpu)
    {
        Gpu = gpu;
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
        m_SpecularShaderHandle = Gpu.ShaderController.Load("Assets/Shaders/specular.shader");
    }
    
    public void Render(IGpu gpu, ICamera camera, ITransform3D light)
    {
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);

        var mesh = gpu.MeshController;
        var shader = gpu.ShaderController;

        gpu.SaveState();
        gpu.EnableBackfaceCulling = true;
        gpu.EnableDepthTest = true;
        
        shader.Bind(m_SpecularShaderHandle);
        shader.SetVector3("light.position", light.WorldPosition);
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
            mesh.Bind(renderGroup.Item1);
            var textures = renderGroup.Item2;
            
            shader.SetTexture2d("material.diffuse", textures.Diffuse);
            shader.SetTexture2d("material.normal_map", textures.Normal);
            shader.SetTexture2d("material.roughness_map", textures.Roughness);
            shader.SetTexture2d("material.occlusion", textures.Occlusion);
            shader.SetTexture2d("material.translucency", textures.Translucency);
            
            var transforms = m_MeshToRenderableMap[renderGroup];
            var transformsCount = transforms.Count;

            using (var writeBuffer = modelMatricesBuffer.Use())
            {
                for (var i = 0; i < transformsCount; i++)
                {
                    var transform = transforms[i];
                    writeBuffer.Put(transform.WorldMatrix);
                }
                
                writeBuffer.Write();
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
    public IGpuTextureHandle Diffuse { get; init; }
    public IGpuTextureHandle Normal { get; init; }
    public IGpuTextureHandle Roughness { get; init; }
    public IGpuTextureHandle Occlusion { get; init; }
    public IGpuTextureHandle Translucency { get; init; }

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