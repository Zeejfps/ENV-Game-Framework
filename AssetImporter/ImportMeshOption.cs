using Assimp;
using Framework.Assets;

public class ImportMeshOption
{
    public void Run()
    {
        Console.Write("Mesh Path: ");
        var path = Console.ReadLine();
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("Invalid path entered!");
            return;
        }
        
        path = path.Replace("\"", "");
        if (!File.Exists(path))
        {
            Console.WriteLine("Invalid path entered!");
            return;
        }
        
        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, 
            PostProcessSteps.CalculateTangentSpace |
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices);
        
        var mesh = scene.Meshes[0];
        Console.Write("Save As: ");
        var saveAs = Console.ReadLine();
        if (string.IsNullOrEmpty(saveAs))
        {
            Console.WriteLine("Invalid save path");
            return;
        }

        saveAs = saveAs.Replace("\"", "");
        var meshAsset = new MeshAsset_GL
        {
            Vertices = mesh.Vertices.Select(v => new List<float>{v.X, v.Y, v.Z}).Aggregate((total, next) =>
            {
                total.AddRange(next);
                return total;
            }).ToArray(),
            
            Normals = mesh.Normals.Select(v => new List<float>{v.X, v.Y, v.Z}).Aggregate((total, next) =>
            {
                total.AddRange(next);
                return total;
            }).ToArray(),
            
            Tangents = mesh.Tangents.Select(v => new List<float>{v.X, v.Y, v.Z}).Aggregate((total, next) =>
            {
                total.AddRange(next);
                return total;
            }).ToArray(),
            
            Triangles = mesh.Faces.Select(v => new List<int>{v.Indices[0], v.Indices[1], v.Indices[2]}).Aggregate((total, next) =>
            {
                total.AddRange(next);
                return total;
            }).ToArray(),
            
            Uvs = mesh.TextureCoordinateChannels[0].Select(v => new List<float>{v.X, v.Y}).Aggregate((total, next) =>
            {
                total.AddRange(next);
                return total;
            }).ToArray(),
        };
        
        using var stream = File.Open(saveAs, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(stream);
        meshAsset.Serialize(writer);
    }
}