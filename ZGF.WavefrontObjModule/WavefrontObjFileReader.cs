namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileReader
{
    private readonly List<VertexPosition> _vertexPositions = new();
    private readonly List<VertexNormal> _vertexNormals = new();
    private readonly List<VertexTextureCoord> _vertexTextureCoords = new();
    private readonly List<Face> _faces = new();
    private readonly List<NamedObjectDefinition> _namedObjects = new();

    private readonly char[] _buffer = new char[256];

    private NamedObjectDefinition? _currentObject;
    private int _vertexPositionIndex = 0;
    private int _vertexNormalsIndex = 0;
    private int _vertexTextureCoordsIndex = 0;
    private int _facesIndex = 0;

    public IWavefrontObjFileContents ReadFromFile(string pathToFile)
    {
        _currentObject = null;
        _vertexPositionIndex = 0;
        _vertexNormalsIndex = 0;
        _vertexTextureCoordsIndex = 0;
        _facesIndex = 0;

        _namedObjects.Clear();
        _vertexPositions.Clear();
        _vertexNormals.Clear();
        _vertexTextureCoords.Clear();
        _faces.Clear();

        using var fileStream = File.OpenRead(pathToFile);
        using var textReader = new StreamReader(fileStream);

        Span<char> buff = stackalloc char[7];
        var len = 0;
        int chasAsInt;
        while ((chasAsInt = textReader.Read()) > 0)
        {
            if (chasAsInt == ' ')
            {
                var header = buff[..len];
                switch (header)
                {
                    case "#":
                        ReadComment(textReader);
                        break;
                    case "mtllib":
                        ReadMaterial(textReader);
                        break;
                    case "o":
                        ReadObject(textReader);
                        break;
                    case "v":
                        ReadVertexPosition(textReader);
                        break;
                    case "vn":
                        ReadVertexNormal(textReader);
                        break;
                    case "vt":
                        ReadTextureCoord(textReader);
                        break;
                    case "s":
                        ReadSmoothingGroup(textReader);
                        break;
                    case "f":
                        ReadFace(textReader);
                        break;
                    default:
                        throw new Exception($"Unexpected header '{header}' encountered while reading obj file");
                }

                len = 0;
                continue;
            }
            
            buff[len] = (char)chasAsInt;
            len++;
        }

        SetObjectData();

        var vertexPositions = _vertexPositions.ToArray();
        var vertexNormals = _vertexNormals.ToArray();
        var vertexTextureCoords = _vertexTextureCoords.ToArray();
        var faces = _faces.ToArray();

        var namedObjects = _namedObjects
            .Select(t => new NamedObject
            {
                Name = t.Name,
                VertexPositions = vertexPositions.AsMemory(t.VertexPositionsRange),
                VertexNormals = vertexNormals.AsMemory(t.VertexNormalsRange),
                VertexTextureCoords = vertexTextureCoords.AsMemory(t.VertexTextureCoordsRange),
                Faces = faces.AsMemory(t.FacesRange),
            })
            .ToArray();

        var data = new SomethingContent
        {
            VertexPositions = vertexPositions,
            VertexNormals = vertexNormals,
            VertexTextureCoords = vertexTextureCoords,
            Faces = faces,
            NamedObjects = namedObjects,
        };

        return new WavefrontObjFileContents(data);
    }

    private void ReadVertexNormal(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;

        Span<float> values = stackalloc float[3];
        var currValueIndex = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == ' ')
            {
                var floatValue = float.Parse(_buffer.AsSpan(0, len));
                values[currValueIndex] = floatValue;
                ++currValueIndex;
                len = 0;
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        var normal = new VertexNormal
        {
            X = values[0],
            Y = values[1],
            Z = values[2],
        };
        _vertexNormals.Add(normal);
    }

    private void ReadTextureCoord(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;

        Span<float> values = stackalloc float[2];
        var currValueIndex = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == ' ')
            {
                var floatValue = float.Parse(_buffer.AsSpan(0, len));
                values[currValueIndex] = floatValue;
                ++currValueIndex;
                len = 0;
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        var normal = new VertexTextureCoord
        {
            U = values[0],
            V = values[1],
        };
        _vertexTextureCoords.Add(normal);
    }

    private void ReadComment(StreamReader textReader)
    {
        int chasAsInt;
        while ((chasAsInt = textReader.Read()) > 0)
        {
            if (chasAsInt == '\r') continue;
            if (chasAsInt == '\n')
            {
                return;
            }
        }
    }

    private void ReadSmoothingGroup(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n')
            {
                break;
            }
        }

        var value = buffer.AsSpan(0, len);
        if (value is "off" or "0")
        {

        }
        else if (int.TryParse(value, out var groupId))
        {

        }
    }

    private void ReadVertexPosition(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;

        Span<float> values = stackalloc float[4];
        var currValueIndex = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == ' ')
            {
                var floatValue = float.Parse(_buffer.AsSpan(0, len));
                values[currValueIndex] = floatValue;
                ++currValueIndex;
                len = 0;
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        if (currValueIndex < 3)
            values[3] = 1.0f;

        var position = new VertexPosition
        {
            X = values[0],
            Y = values[1],
            Z = values[2],
            W = values[3]
        };
        _vertexPositions.Add(position);
    }

    private void ReadMaterial(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n')
            {
                break;
            }

            buffer[len] = (char)charAsInt;
            len++;
        }

        var materialName = buffer.AsSpan(0, len);
    }

    private void ReadFace(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        
        Span<int> indices = stackalloc int[3];
        Span<Triangle> trianglesBuffer = stackalloc Triangle[4];
        var triangleCount = 0;
        var indexCount = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '/')
            {
                indices[indexCount] = int.Parse(buffer.AsSpan(0, len));
                indexCount++;
                len = 0;
                continue;
            }
            
            if (charAsInt == ' ')
            {
                indices[indexCount] = int.Parse(buffer.AsSpan(0, len));
                trianglesBuffer[triangleCount] = new Triangle
                {
                    V0 = indices[0],
                    V1 = indices[1],
                    V2 = indices[2]
                };
                triangleCount++;
                
                len = 0;
                indexCount = 0;

                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }

        var face = new Face
        {
            Triangles = trianglesBuffer
                .Slice(0, triangleCount)
                .ToArray()
        };
        
        _faces.Add(face);
    }

    private void SetObjectData()
    {
        var obj = _currentObject;
        if (obj != null)
        {
            obj.VertexPositionsRange = new Range(_vertexPositionIndex, _vertexPositions.Count);
            _vertexPositionIndex += _vertexPositions.Count;

            obj.VertexPositionsRange = new Range(_vertexNormalsIndex, _vertexPositions.Count);
            _vertexNormalsIndex += _vertexNormals.Count;

            obj.VertexTextureCoordsRange = new Range(_vertexTextureCoordsIndex, _vertexTextureCoords.Count);
            _vertexTextureCoordsIndex += _vertexTextureCoords.Count;

            obj.FacesRange = new Range(_facesIndex, _faces.Count);
            _facesIndex += _faces.Count;
        }
    }
    
    private void ReadObject(StreamReader textReader)
    {
        SetObjectData();

        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        var objName = buffer.AsSpan(0, len);
        var namedObject = new NamedObjectDefinition()
        {
            Name = new string(objName),
        };
        _namedObjects.Add(namedObject);
    }
}