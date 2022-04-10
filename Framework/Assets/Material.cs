using System.Numerics;

namespace Framework;

public class Material : IMaterial
{
    public string Shader { get; }
    public bool IsLoaded { get; private set; }

    public Material(string shader)
    {
        Shader = shader;
        IsLoaded = true;
    }

    private readonly HashSet<MaterialProperty> m_AllProperties = new();
    private readonly Dictionary<string, Vector3Property> m_Vector3Properties = new();
    private readonly Dictionary<string, FloatProperty> m_FloatProperties = new();
    private readonly Dictionary<string, Matrix4x4Property> m_Matrix4X4Properties = new();
    private readonly Dictionary<string, TextureProperty> m_TextureProperties = new();

    public void SetVector3(string propertyName, Vector3 value)
    {
        if (!m_Vector3Properties.TryGetValue(propertyName, out var property))
        {
            property = new Vector3Property(propertyName);
            m_AllProperties.Add(property);
            m_Vector3Properties.Add(propertyName, property);
        }

        property.Value = value;
    }

    public void SetMatrix4x4(string propertyName, Matrix4x4 matrix)
    {
        if (!m_Matrix4X4Properties.TryGetValue(propertyName, out var property))
        {
            property = new Matrix4x4Property(propertyName);
            m_AllProperties.Add(property);
            m_Matrix4X4Properties.Add(propertyName, property);
        }

        property.Value = matrix;
    }

    public void SetFloat(string propertyName, float x)
    {
        if (!m_FloatProperties.TryGetValue(propertyName, out var property))
        {
            property = new FloatProperty(propertyName);
            m_AllProperties.Add(property);
            m_FloatProperties.Add(propertyName, property);
        }

        property.Value = x;
    }

    public void SetVector3(string propertyName, float x, float y, float z)
    {
        SetVector3(propertyName, new Vector3(x, y, z));
    }

    public void SetTexture2d(string propertyName, ITexture texture)
    {
        if (!m_TextureProperties.TryGetValue(propertyName, out var property))
        {
            property = new TextureProperty(propertyName);
            m_AllProperties.Add(property);
            m_TextureProperties.Add(propertyName, property);
        }

        property.Value = texture;
    }
    
    public void SetMatrix4x4(string propertyName, float[] matrix)
    {
        SetMatrix4x4(propertyName, matrix.ToMatrix4x4());
    }

    public void Apply(IShaderProgram shaderProgram)
    {
        foreach (var property in m_AllProperties)
            property.Apply(shaderProgram);
    }
    
    public void Unload()
    {
        IsLoaded = false;
    }
}

abstract class MaterialProperty
{
    public string Name { get; }
    
    public MaterialProperty(string name)
    {
        Name = name;
    }

    public abstract void Apply(IShaderProgram shaderProgram);
}

abstract class MaterialProperty<T> : MaterialProperty
{
    public T Value { get; set; }
    
    protected MaterialProperty(string name) : base(name) { }
}

class Vector3Property : MaterialProperty<Vector3>
{
    public Vector3Property(string name) : base(name) { }
    
    public override void Apply(IShaderProgram shaderProgram)
    {
        shaderProgram.SetVector3f(Name, Value.X, Value.Y, Value.Z);
    }
}
class FloatProperty : MaterialProperty<float>
{
    public FloatProperty(string name) : base(name) { }
    
    public override void Apply(IShaderProgram shaderProgram)
    { 
        shaderProgram.SetFloat(Name, Value);
    }
}
class Matrix4x4Property : MaterialProperty<Matrix4x4>
{
    public Matrix4x4Property(string name) : base(name)
    {
    }

    public override void Apply(IShaderProgram shaderProgram)
    {
        shaderProgram.SetMatrix4x4f(Name, Value.ToFloatArray());
    }
}

class TextureProperty : MaterialProperty<ITexture>
{
    public TextureProperty(string name) : base(name)
    {
    }

    public override void Apply(IShaderProgram shaderProgram)
    {
        shaderProgram.SetTexture2d(Name, Value);
    }
}

static class Matrix4x4Ext
{
    public static float[] ToFloatArray(this Matrix4x4 matrix)
    {
        var array = new float[4*4];
        
        array[00] = matrix.M11;
        array[01] = matrix.M12;
        array[02] = matrix.M13;
        array[03] = matrix.M14;
        
        array[04] = matrix.M21;
        array[05] = matrix.M22;
        array[06] = matrix.M23;
        array[07] = matrix.M24;
        
        array[08] = matrix.M31;
        array[09] = matrix.M32;
        array[10] = matrix.M33;
        array[11] = matrix.M34;
        
        array[12] = matrix.M41;
        array[13] = matrix.M42;
        array[14] = matrix.M43;
        array[15] = matrix.M44;

        return array;
    }
    
    public static Matrix4x4 ToMatrix4x4(this float[] array)
    {
        var matrix = new Matrix4x4(
            array[00],
            array[01],
            array[02],
            array[03],
            array[04],
            array[05],
            array[06],
            array[07],
            array[08],
            array[09],
            array[10],
            array[11],
            array[12],
            array[13],
            array[14],
            array[15]
        );

        return matrix;
    }
}
