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
    private IShaderStorageBufferHandle m_ModelMatrixBuffer;

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
        m_ModelMatrixBuffer = Gpu.BufferController.CreateAndBindShaderStorageBuffer(BufferUsage.DynamicDraw, sizeof(float) * 16 * 1000);
        Gpu.ShaderController.AttachBuffer("model_matrices_t", 0, m_ModelMatrixBuffer);
    }
    
    public void Render(IGpu gpu, ICamera camera, ITransform3D light)
    {
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);

        var mesh = gpu.MeshController;
        var shaderController = gpu.ShaderController;
        var bufferController = gpu.BufferController;

        gpu.SaveState();
        gpu.EnableBackfaceCulling = true;
        gpu.EnableDepthTest = true;
        
        shaderController.Bind(m_SpecularShaderHandle);
        shaderController.SetVector3("light.position", light.WorldPosition);
        shaderController.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderController.SetMatrix4x4("matrix_view", viewMatrix);
        shaderController.SetVector3("camera_position", camera.Transform.WorldPosition);
        shaderController.SetVector3("light.diffuse", _lightColor);
        shaderController.SetVector3("light.specular", _specularColor);
        shaderController.SetVector3("light.ambient", _ambientColor);
        shaderController.SetFloat("material.shininess", _shininess);
        
        foreach (var renderGroup in m_MeshToRenderableMap.Keys)
        {
            mesh.Bind(renderGroup.Item1);
            var textures = renderGroup.Item2;
            
            shaderController.SetTexture2d("material.diffuse", textures.Diffuse);
            shaderController.SetTexture2d("material.normal_map", textures.Normal);
            shaderController.SetTexture2d("material.roughness_map", textures.Roughness);
            shaderController.SetTexture2d("material.occlusion", textures.Occlusion);
            shaderController.SetTexture2d("material.translucency", textures.Translucency);
            
            var transforms = m_MeshToRenderableMap[renderGroup];
            var transformsCount = transforms.Count;

            bufferController.Bind(m_ModelMatrixBuffer);
            var modelMatrices = new Matrix4x4[transforms.Count]; 
            for (var i = 0; i < transformsCount; i++)
            {
                var transform = transforms[i];
                modelMatrices[i] = transform.WorldMatrix;
            }
            
            bufferController.Upload<Matrix4x4>(modelMatrices.AsSpan());

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