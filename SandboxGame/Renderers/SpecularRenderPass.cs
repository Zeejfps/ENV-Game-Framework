﻿using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Framework;

public class SpecularRenderPass
{
    private readonly Dictionary<(IMesh, SpecularRenderableTextures), List<Matrix4x4>> m_MeshToRenderableMap = new();

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

    public void Submit(in SpecularRenderable renderable)
    {
        var mesh = renderable.Mesh;
        var textures = renderable.Textures;

        var key = (mesh, textures);
        if (!m_MeshToRenderableMap.TryGetValue(key, out var renderables))
        {
            renderables = new List<Matrix4x4>();
            m_MeshToRenderableMap[key] = renderables;
        }
        
        renderables.Add(renderable.WorldMatrix);
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_SpecularMaterial = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/specular.material");
        m_SpecularMaterial.EnableBackfaceCulling = true;
        m_SpecularMaterial.EnableDepthTest = true;
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

        var modelMatricesBuffer = material.GetBuffer("model_matrices_t");
        
        foreach (var renderGroup in m_MeshToRenderableMap.Keys)
        {
            using var mesh = renderGroup.Item1.Use();
            var textures = renderGroup.Item2;
            
            material.SetTexture2d("material.diffuse", textures.Diffuse);
            material.SetTexture2d("material.normal_map", textures.Normal);
            material.SetTexture2d("material.roughness_map", textures.Roughness);
            material.SetTexture2d("material.occlusion", textures.Occlusion);
            material.SetTexture2d("material.translucency", textures.Translucency);
            
            var transforms = m_MeshToRenderableMap[renderGroup];

            using (var buffer = modelMatricesBuffer.Use())
            {
                buffer.Clear();
                buffer.Put(CollectionsMarshal.AsSpan(transforms));
                buffer.Apply();
            }

            mesh.RenderInstanced(transforms.Count);
            m_MeshToRenderableMap[renderGroup].Clear();
        }
    }
    
    public void Unload(IScene scene)
    {
        Debug.Assert(m_SpecularMaterial != null);
        m_SpecularMaterial.Unload();
        m_SpecularMaterial = null;
    }
}

public readonly struct SpecularRenderable 
{
    public IMesh Mesh { get; init; }
    public Matrix4x4 WorldMatrix { get; init; }
    public SpecularRenderableTextures Textures { get; init; }
}

public struct SpecularRenderableTextures : IEquatable<SpecularRenderableTextures>
{
    public ITexture Diffuse { get; init; }
    public ITexture Normal { get; init; }
    public ITexture Roughness { get; init; }
    public ITexture Occlusion { get; init; }
    public ITexture Translucency { get; init; }

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