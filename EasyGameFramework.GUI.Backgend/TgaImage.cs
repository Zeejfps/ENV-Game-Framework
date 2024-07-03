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
        byte[] pixels;
        using (BinaryReader reader = new BinaryReader(File.Open(PathToFile, FileMode.Open)))
        {
            // Read the TGA header
            byte[] header = reader.ReadBytes(18);
            int imageType = header[2];

            if (imageType != 2 && imageType != 3)
            {
                throw new Exception("Unsupported TGA format.");
            }

            width = BitConverter.ToInt16(header, 12);
            height = BitConverter.ToInt16(header, 14);
            int bitsPerPixel = header[16];

            // Read the image data
            var dataSize = width * height * (bitsPerPixel / 8);

            uint uploadBufferId;
            GL46.glGenBuffers(1, &uploadBufferId);
            OpenGlUtils.AssertNoGlError();
            GL46.glBindBuffer(GL46.GL_PIXEL_UNPACK_BUFFER, uploadBufferId);
            OpenGlUtils.AssertNoGlError();

            GL46.glBufferData(GL46.GL_PIXEL_UNPACK_BUFFER, new IntPtr(dataSize), (void*)0, GL46.GL_STATIC_DRAW);
            OpenGlUtils.AssertNoGlError();
            
            var ptrToBuffer = GL46.glMapBuffer(GL46.GL_PIXEL_UNPACK_BUFFER, GL46.GL_WRITE_ONLY);
            OpenGlUtils.AssertNoGlError();
            
            var buffer = new Span<byte>(ptrToBuffer, dataSize);
            reader.Read(buffer);
            
            GL46.glUnmapBuffer(GL46.GL_PIXEL_UNPACK_BUFFER);
            OpenGlUtils.AssertNoGlError();

            //Console.WriteLine("Image Type: " + imageType);
            //Console.WriteLine("Pixels: " + buffer.Length);
            
            // If the image is stored upside down, you may need to flip it

            GL46.glTexImage2D(GL46.GL_TEXTURE_2D, 0, GL46.GL_RGBA8, width, height, 0, GL46.GL_RED, GL46.GL_UNSIGNED_BYTE, OpenGlUtils.Offset(0));
            OpenGlUtils.AssertNoGlError();
            
            GL46.glDeleteBuffers(1, &uploadBufferId);
        }
    }
}