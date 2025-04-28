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
        var width = 0;
        var height = 0;
        using (BinaryReader reader = new BinaryReader(File.Open(PathToFile, FileMode.Open)))
        {
            // Read the TGA header
            byte[] header = reader.ReadBytes(18);
            int imageType = header[2];

            width = BitConverter.ToInt16(header, 12);
            height = BitConverter.ToInt16(header, 14);
            int bitsPerPixel = header[16];
            var bytesPerPixel = bitsPerPixel / 8;

            var dataSize = width * height * bytesPerPixel;

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
            else
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
            
            GL46.glUnmapBuffer(GL46.GL_PIXEL_UNPACK_BUFFER);
            OpenGlUtils.AssertNoGlError();

            //Console.WriteLine("Image Type: " + imageType);
            //Console.WriteLine("Pixels: " + buffer.Length);
            
            // If the image is stored upside down, you may need to flip it

            GL46.glTexImage2D(GL46.GL_TEXTURE_2D, 0, (int)GL46.GL_RGBA8, width, height, 0, GL46.GL_RED, GL46.GL_UNSIGNED_BYTE, OpenGlUtils.Offset(0));
            OpenGlUtils.AssertNoGlError();
            
            GL46.glDeleteBuffers(1, &uploadBufferId);
        }
    }
}