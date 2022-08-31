using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.OpenGL;

public class CpuMesh : ICpuMesh
{
    public float[] Vertices { get; set; } = Array.Empty<float>();
    public float[] Uvs { get; set; } = Array.Empty<float>();
    public float[] Normals { get; set; } = Array.Empty<float>();
    public float[] Tangents { get; set; } = Array.Empty<float>();
    public int[] Triangles { get; set; } = Array.Empty<int>();

    public void Serialize(BinaryWriter writer)
    {
        WriteArray(writer, Vertices);
        WriteArray(writer, Uvs);
        WriteArray(writer, Normals);
        WriteArray(writer, Tangents);
        WriteArray(writer, Triangles);
    }

    public static CpuMesh Deserialize(BinaryReader reader)
    {
        //Console.WriteLine("Reading Vertices");
        var vertices = ReadFloatArray(reader);

        //Console.WriteLine("Reading Uvs");
        var uvs = ReadFloatArray(reader);

        //Console.WriteLine("Reading Normals");
        var normals = ReadFloatArray(reader);

        //Console.WriteLine("Reading Tangents");
        var tangents = ReadFloatArray(reader);

        //Console.WriteLine("Reading Triangles");
        var triangles = ReadIntArray(reader);

        return new CpuMesh
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
        //Console.WriteLine($"Reading Array of size: {length}");
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

    public void Dispose()
    {
        Vertices = Array.Empty<float>();
        Uvs = Array.Empty<float>();
        Normals = Array.Empty<float>();
        Tangents = Array.Empty<float>();
        Triangles = Array.Empty<int>();
    }
}