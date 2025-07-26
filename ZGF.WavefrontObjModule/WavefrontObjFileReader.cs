using System.Globalization;

namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileReader
{
    private readonly char[] _buffer = new char[256];

    private readonly List<VertexPosition> _vertexPositions = new();
    private readonly List<VertexNormal> _vertexNormals = new();
    private readonly List<VertexTextureCoord> _vertexTextureCoords = new();
    private readonly List<Face> _faces = new();
    private readonly List<NamedObjectDefinition> _namedObjects = new();
    private readonly List<SmoothingGroupDefinition> _smoothingGroups = new();
    private readonly HashSet<string> _mtlFileNames = new();

    private NamedObjectDefinition? _currentObject;
    private SmoothingGroupDefinition? _currentSmoothingGroup;

    private int _smoothingGroupIndex;
    private int _vertexPositionIndex;
    private int _vertexNormalsIndex;
    private int _vertexTextureCoordsIndex;
    private int _facesIndex;

    public IWavefrontObjFileContents ReadFromFile(string pathToFile)
    {
        _currentObject = null;
        _vertexPositionIndex = 0;
        _vertexNormalsIndex = 0;
        _vertexTextureCoordsIndex = 0;
        _smoothingGroupIndex = 0;
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
        while ((chasAsInt = textReader.Read()) != -1)
        {
            if (chasAsInt == ' ')
            {
                if (len > 0)
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
                            PushCurrentObject();
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
                            PushCurrentSmoothingGroup();
                            ReadSmoothingGroup(textReader);
                            break;
                        case "f":
                            ReadFace(textReader);
                            break;
                        default:
                            throw new Exception($"Unexpected header '{header}' encountered while reading obj file");
                    }
                    len = 0;
                }
                continue;
            }
            
            buff[len] = (char)chasAsInt;
            len++;
        }

        PushCurrentObject();

        var vertexPositions = _vertexPositions.ToArray();
        var vertexNormals = _vertexNormals.ToArray();
        var vertexTextureCoords = _vertexTextureCoords.ToArray();
        var faces = _faces.ToArray();

        var namedObjects = _namedObjects
            .Select(t => new Object
            {
                Name = t.Name,
                VertexPositions = vertexPositions.AsMemory(t.VertexPositionsRange),
                VertexNormals = vertexNormals.AsMemory(t.VertexNormalsRange),
                VertexTextureCoords = vertexTextureCoords.AsMemory(t.VertexTextureCoordsRange),
                Faces = faces.AsMemory(t.FacesRange),
            })
            .ToArray();

        var smoothingGroups = _smoothingGroups
            .Select(t => new SmoothingGroup
            {
                Id = t.Id,
                Faces = faces.AsMemory(t.FacesRange),
            })
            .ToArray();

        var data = new RawModelData
        {
            VertexPositions = vertexPositions,
            VertexNormals = vertexNormals,
            VertexTextureCoords = vertexTextureCoords,
            Faces = faces,
            Objects = namedObjects,
            SmoothingGroups = smoothingGroups,
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
        while ((charAsInt = textReader.Read()) != -1)
        {
            if (charAsInt == ' ')
            {
                if (len > 0)
                {
                    var floatValue = float.Parse(
                        _buffer.AsSpan(0, len),
                        CultureInfo.InvariantCulture
                    );
                    values[currValueIndex] = floatValue;
                    ++currValueIndex;
                }
                len = 0;
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        if (len > 0)
        {
            var floatValue = float.Parse(
                _buffer.AsSpan(0, len),
                CultureInfo.InvariantCulture
            );
            values[currValueIndex] = floatValue;
            ++currValueIndex;
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
        while ((charAsInt = textReader.Read()) != -1)
        {
            if (charAsInt == ' ')
            {
                if (len > 0)
                {
                    var floatValue = float.Parse(
                        _buffer.AsSpan(0, len),
                        CultureInfo.InvariantCulture
                    );
                    values[currValueIndex] = floatValue;
                    ++currValueIndex;
                    len = 0;
                }
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        if (len > 0)
        {
            var floatValue = float.Parse(
                _buffer.AsSpan(0, len),
                CultureInfo.InvariantCulture
            );
            values[currValueIndex] = floatValue;
            ++currValueIndex;
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
        while ((chasAsInt = textReader.Read()) != -1)
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
        while ((charAsInt = textReader.Read()) != -1)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n')
            {
                break;
            }
            buffer[len] = (char)charAsInt;
            len++;
        }

        var value = buffer.AsSpan(0, len);
        var isOff = true;
        if (int.TryParse(value, out var groupId))
        {
            isOff = groupId == 0;
        }

        if (isOff)
        {
            _currentSmoothingGroup = null;
        }
        else
        {
            var newSmoothingGroup = new SmoothingGroupDefinition
            {
                Id = groupId,
            };
            _currentSmoothingGroup = newSmoothingGroup;
        }
    }

    private void PushCurrentSmoothingGroup()
    {
        var sg = _currentSmoothingGroup;
        if (sg != null)
        {
            sg.FacesRange = new Range(_smoothingGroupIndex, _faces.Count);
            _smoothingGroups.Add(sg);
        }
        _smoothingGroupIndex = _faces.Count;
    }

    private void ReadVertexPosition(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;

        Span<float> values = stackalloc float[4];
        var currValueIndex = 0;
        while ((charAsInt = textReader.Read()) != -1)
        {
            if (charAsInt == ' ')
            {
                if (len > 0)
                {
                    var floatValue = float.Parse(
                        _buffer.AsSpan(0, len),
                        CultureInfo.InvariantCulture
                    );
                    values[currValueIndex] = floatValue;
                    ++currValueIndex;
                    len = 0;
                }
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        if (len > 0)
        {
            var floatValue = float.Parse(
                _buffer.AsSpan(0, len),
                CultureInfo.InvariantCulture
            );
            values[currValueIndex] = floatValue;
            ++currValueIndex;
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
        while ((charAsInt = textReader.Read()) != -1)
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
        _mtlFileNames.Add(materialName.ToString());
    }

    private void ReadFace(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        
        Span<int> indices = stackalloc int[3];
        Span<Vertex> vertexBuffer = stackalloc Vertex[4];
        var vertexCount = 0;
        var indexCount = 0;
        while ((charAsInt = textReader.Read()) != -1)
        {
            if (charAsInt == '/')
            {
                if (len > 0)
                {
                    indices[indexCount] = int.Parse(
                        buffer.AsSpan(0, len)
                    );
                }
                indexCount++;
                len = 0;
                continue;
            }
            
            if (charAsInt == ' ')
            {
                indices[indexCount] = int.Parse(buffer.AsSpan(0, len));
                vertexBuffer[vertexCount] = new Vertex
                {
                    PositionIndex = indices[0],
                    TextureCoordIndex = indices[1],
                    NormalIndex = indices[2]
                };
                vertexCount++;
                
                len = 0;
                indexCount = 0;

                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }

        if (len > 0)
        {
            indices[indexCount] = int.Parse(
                buffer.AsSpan(0, len)
            );
            vertexBuffer[vertexCount] = new Vertex
            {
                PositionIndex = indices[0],
                TextureCoordIndex = indices[1],
                NormalIndex = indices[2]
            };
            vertexCount++;
        }

        var face = new Face
        {
            Vertices = vertexBuffer
                .Slice(0, vertexCount)
                .ToArray()
        };
        
        _faces.Add(face);
    }

    private void PushCurrentObject()
    {
        var obj = _currentObject;
        if (obj == null)
            return;

        obj.VertexPositionsRange = new Range(_vertexPositionIndex, _vertexPositions.Count);
        obj.VertexNormalsRange = new Range(_vertexNormalsIndex, _vertexNormals.Count);
        obj.VertexTextureCoordsRange = new Range(_vertexTextureCoordsIndex, _vertexTextureCoords.Count);
        obj.FacesRange = new Range(_facesIndex, _faces.Count);
        _namedObjects.Add(obj);

        _vertexPositionIndex = _vertexPositions.Count;
        _vertexNormalsIndex = _vertexNormals.Count;
        _vertexTextureCoordsIndex = _vertexTextureCoords.Count;
        _facesIndex = _faces.Count;
    }
    
    private void ReadObject(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) != -1)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;

            buffer[len] = (char)charAsInt;
            len++;
        }

        var objName = buffer.AsSpan(0, len);
        var namedObject = new NamedObjectDefinition
        {
            Name = new string(objName),
        };
        _currentObject = namedObject;
    }
}