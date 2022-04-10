namespace Framework.Assets;

public class MeshAsset_GL
{
    public float[] Vertices { get; init; }
    public float[] Uvs { get; init; }
    public float[] Normals { get; init; }
    public float[] Tangents { get; init; }
    public int[] Triangles { get; init; }

    public void Serialize(BinaryWriter writer)
    {
        WriteArray(writer, Vertices);
        WriteArray(writer, Uvs);
        WriteArray(writer, Normals);
        WriteArray(writer, Tangents);
        WriteArray(writer, Triangles);
    }

    public static MeshAsset_GL Deserialize(BinaryReader reader)
    {
        var vertices = ReadFloatArray(reader);
        var uvs = ReadFloatArray(reader);
        var normals = ReadFloatArray(reader);
        var tangents = ReadFloatArray(reader);
        var triangles = ReadIntArray(reader);
        
        return new MeshAsset_GL
        {
            Vertices = vertices,
            Uvs = uvs,
            Normals = normals,
            Tangents = tangents,
            Triangles = triangles
        };
    }

    private void WriteArray(BinaryWriter writer, float[] array)
    {
        var length = array.Length;
        writer.Write(length);
        for (var i = 0; i < length; i++)
            writer.Write(array[i]);
    }
    
    private static void WriteArray(BinaryWriter writer, int[] array)
    {
        var length = array.Length;
        writer.Write(length);
        for (var i = 0; i < length; i++)
            writer.Write(array[i]);
    }
    
    private static float[] ReadFloatArray(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var data = new float[length];
        for (var i = 0; i < length; i++)
            data[i] = reader.ReadSingle();
        return data;
    }
    
    private static int[] ReadIntArray(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var data = new int[length];
        for (var i = 0; i < length; i++)
            data[i] = reader.ReadInt32();
        return data;
    }
}