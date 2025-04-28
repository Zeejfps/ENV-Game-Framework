namespace OpenGLSandbox;

public sealed class TgaImage
{
    private string PathToFile { get; }
    
    public TgaImage(string pathToFile)
    {
        PathToFile = pathToFile;
    }

    public unsafe void UploadToGpu()
    {
        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(PathToFile, FileMode.Open)))
            {
                // Read the TGA header properly
                byte idLength = reader.ReadByte();
                byte colorMapType = reader.ReadByte();
                byte imageType = reader.ReadByte();

                // Color map specification
                ushort colorMapOrigin = reader.ReadUInt16();
                ushort colorMapLength = reader.ReadUInt16();
                byte colorMapDepth = reader.ReadByte();

                // Image specification
                ushort xOrigin = reader.ReadUInt16();
                ushort yOrigin = reader.ReadUInt16();
                ushort width = reader.ReadUInt16();
                ushort height = reader.ReadUInt16();
                byte bitsPerPixel = reader.ReadByte();
                byte imageDescriptor = reader.ReadByte();

                // Skip the ID field if present
                if (idLength > 0)
                {
                    reader.ReadBytes(idLength);
                }

                // Skip the color map data if present
                if (colorMapType == 1)
                {
                    int colorMapBytes = (colorMapLength * colorMapDepth + 7) / 8;
                    reader.ReadBytes(colorMapBytes);
                }

                var bytesPerPixel = (bitsPerPixel + 7) / 8; // This handles non-multiples of 8 correctly
                var dataSize = width * height * bytesPerPixel;

                // Configure OpenGL format based on the TGA format
                uint glInternalFormat;
                uint glFormat;
                uint glType;

                switch (bitsPerPixel)
                {
                    case 8:
                        glInternalFormat = GL46.GL_R8;
                        glFormat = GL46.GL_RED;
                        glType = GL46.GL_UNSIGNED_BYTE;
                        break;
                    case 16:
                        glInternalFormat = GL46.GL_RG8;
                        glFormat = GL46.GL_RG;
                        glType = GL46.GL_UNSIGNED_BYTE;
                        break;
                    case 24:
                        glInternalFormat = GL46.GL_RGB8;
                        glFormat = GL46.GL_BGR;  // TGA stores colors as BGR
                        glType = GL46.GL_UNSIGNED_BYTE;
                        break;
                    case 32:
                        glInternalFormat = GL46.GL_RGBA8;
                        glFormat = GL46.GL_BGRA; // TGA stores colors as BGRA
                        glType = GL46.GL_UNSIGNED_BYTE;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported TGA bit depth: {bitsPerPixel}");
                }

                uint uploadBufferId;
                GL46.glGenBuffers(1, &uploadBufferId);
                OpenGlUtils.AssertNoGlError();
                GL46.glBindBuffer(GL46.GL_PIXEL_UNPACK_BUFFER, uploadBufferId);
                OpenGlUtils.AssertNoGlError();

                GL46.glBufferData(GL46.GL_PIXEL_UNPACK_BUFFER, new IntPtr(dataSize), (void*)0, GL46.GL_STATIC_DRAW);
                OpenGlUtils.AssertNoGlError();
                
                var ptrToBuffer = GL46.glMapBuffer(GL46.GL_PIXEL_UNPACK_BUFFER, GL46.GL_WRITE_ONLY);
                OpenGlUtils.AssertNoGlError();
                
                var pixels = new Span<byte>(ptrToBuffer, dataSize);
                
                if (imageType == 2 || imageType == 3)
                {
                    reader.Read(pixels);
                }
                else if (imageType == 10 || imageType == 11)
                {
                    // RLE compressed
                    int pixelIndex = 0;
                    byte[] color = new byte[bytesPerPixel]; // reuse this buffer

                    while (pixelIndex < pixels.Length)
                    {
                        byte packetHeader = reader.ReadByte();
                        int count = (packetHeader & 0x7F) + 1;

                        if ((packetHeader & 0x80) != 0)
                        {
                            // RLE - next color is repeated count times
                            reader.Read(color, 0, bytesPerPixel);
                            for (int i = 0; i < count; i++)
                            {
                                // Write 'color' into the pixel span
                                color.CopyTo(pixels.Slice(pixelIndex, bytesPerPixel));
                                pixelIndex += bytesPerPixel;
                            }
                        }
                        else
                        {
                            // Raw - next count pixels are raw data
                            int rawSize = count * bytesPerPixel;
                            reader.Read(pixels.Slice(pixelIndex, rawSize));
                            pixelIndex += rawSize;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unsupported TGA image type: {imageType}");
                }
                
                GL46.glUnmapBuffer(GL46.GL_PIXEL_UNPACK_BUFFER);
                OpenGlUtils.AssertNoGlError();

                GL46.glTexImage2D(GL46.GL_TEXTURE_2D, 0, (int)glInternalFormat, width, height, 0, glFormat, glType, OpenGlUtils.Offset(0));
                OpenGlUtils.AssertNoGlError();
                
                GL46.glDeleteBuffers(1, &uploadBufferId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading TGA image: {ex.Message}");
            throw;
        }
    }
}