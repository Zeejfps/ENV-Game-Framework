namespace Framework;

public static class OBJLoader
{
    struct Position
    {
        public float x;
        public float y;
        public float z;
    }

    struct Uv
    {
        public float u;
        public float v;
    }

    struct Normal
    {
        public float x;
        public float y;
        public float z;
    }

    struct Vertex : IEquatable<Vertex>
    {
        public int positionIndex;
        public int uvIndex;
        public int normalIndex;

        public bool Equals(Vertex other)
        {
            return positionIndex == other.positionIndex && normalIndex == other.normalIndex && uvIndex == other.uvIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is Vertex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(positionIndex, normalIndex, uvIndex);
        }

        public static bool operator ==(Vertex left, Vertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vertex left, Vertex right)
        {
            return !left.Equals(right);
        }
    }

    class Face
    {
        public Vertex[] vertices;
    }
    
    public static IMesh LoadObjFromFile(string pathToFile)
    {
        var objPositions = new List<Position>();
        var objUvs = new List<Uv>();
        var objNormals = new List<Normal>();
        var objFaces = new List<Face>();
        
        var lines = File.ReadAllLines(pathToFile);
        foreach (var line in lines)
        {
            if (line.StartsWith("v "))
                objPositions.Add(ParseVertex(line));
            else if (line.StartsWith("vt"))
                objUvs.Add(ParseUv(line));
            else if (line.StartsWith("vn"))
                objNormals.Add(ParseNormal(line));
            else if (line.StartsWith("f"))
                objFaces.Add(ParseFace(line));
        }
        
        var meshVertices = new List<float>();
        var meshUvs = new List<float>();
        var meshNormals = new List<float>();
        var meshTriangles = new List<int>();

        var vertices = new List<Vertex>();

        var faceIndex = 0;
        foreach (var face in objFaces)
        {
            foreach (var vertex in face.vertices)
            {
                var index = vertices.IndexOf(vertex);
                if (index < 0)
                {
                    index = vertices.Count;
                    vertices.Add(vertex);

                    var position = objPositions[vertex.positionIndex];
                    var uv = objUvs[vertex.uvIndex];
                    var normal = objNormals[vertex.normalIndex];
                    
                    meshVertices.Add(position.x);
                    meshVertices.Add(position.y);
                    meshVertices.Add(position.z);
                    
                    meshUvs.Add(uv.u);
                    meshUvs.Add(uv.v);
                    
                    meshNormals.Add(normal.x);
                    meshNormals.Add(normal.y);
                    meshNormals.Add(normal.z);
                }
                meshTriangles.Add(index);
            }
            faceIndex++;
        }

        var mesh = new Mesh
        {
            Vertices = meshVertices.ToArray(),
            Uvs = meshUvs.ToArray(),
            Normals = meshNormals.ToArray(),
            Triangles = meshTriangles.ToArray(),
        };
        return mesh;
    }

    private static Position ParseVertex(string line)
    {
        Span<float> data = stackalloc float[3];
        var tokens = line.Split(' ');
        for (var i = 1; i < tokens.Length; i++)
            data[i - 1] = float.Parse(tokens[i]);
        return new Position
        {
            x = data[0],
            y = data[1],
            z = data[2],
        };
    }
    
    private static Uv ParseUv(string line)
    {
        Span<float> data = stackalloc float[2];
        var tokens = line.Split(' ');
        for (var i = 1; i < tokens.Length; i++)
            data[i - 1] = float.Parse(tokens[i]);
        return new Uv
        {
            u = data[0],
            v = data[1],
        };
    }
    
    private static Normal ParseNormal(string line)
    {
        Span<float> data = stackalloc float[3];
        var tokens = line.Split(' ');
        for (var i = 1; i < tokens.Length; i++)
            data[i - 1] = float.Parse(tokens[i]);
        return new Normal
        {
            x = data[0],
            y = data[1],
            z = data[2],
        };
    }
    
    private static Face ParseFace(string line)
    {
        var vertices = new List<Vertex>();
        var tokens = line.Split(' ');
        for (var i = 1; i < tokens.Length; i++)
        {
            var indices = tokens[i].Split('/');
            
            var vertex = new Vertex
            {
                positionIndex = int.Parse(indices[0]) - 1,
                uvIndex = int.Parse(indices[1]) - 1,
                normalIndex = int.Parse(indices[2]) - 1
            };
            
            vertices.Add(vertex);
        }

        return new Face
        {
            vertices = vertices.ToArray()
        };
    }
}