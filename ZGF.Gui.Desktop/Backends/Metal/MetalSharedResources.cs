using System.Runtime.InteropServices;
using ZGF.Fonts;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Desktop.Backends.Metal;

public sealed unsafe class MetalSharedResources : IDisposable
{
    public IntPtr Device { get; }
    public IntPtr CommandQueue { get; }
    public IntPtr RectPipeline { get; }
    public IntPtr GlyphPipeline { get; }
    public IntPtr ImagePipeline { get; }
    public IntPtr ShadowPipeline { get; }
    public IntPtr UnitQuadBuffer { get; }
    public IntPtr SamplerState { get; }
    public IntPtr AtlasTexture { get; }
    public MetalImageManager ImageManager { get; }
    public FreeTypeFontBackend Fonts { get; }

    private bool _isDisposed;

    public MetalSharedResources(IntPtr device, IntPtr commandQueue, FreeTypeFontBackend fonts, MetalImageManager imageManager)
    {
        Device = device;
        CommandQueue = commandQueue;
        Fonts = fonts;
        ImageManager = imageManager;

        var library = LoadLibrary(device, ShaderAssets.LoadShaderSource("canvas_rect.gen.metal"));
        RectPipeline = BuildPipeline(device, library, MetalRenderedCanvas.MakeRectVertexDescriptor());
        Release(library);

        library = LoadLibrary(device, ShaderAssets.LoadShaderSource("canvas_glyph.gen.metal"));
        GlyphPipeline = BuildPipeline(device, library, MetalRenderedCanvas.MakeGlyphVertexDescriptor());
        Release(library);

        library = LoadLibrary(device, ShaderAssets.LoadShaderSource("canvas_image.gen.metal"));
        ImagePipeline = BuildPipeline(device, library, MetalRenderedCanvas.MakeImageVertexDescriptor());
        Release(library);

        library = LoadLibrary(device, ShaderAssets.LoadShaderSource("canvas_shadow.gen.metal"));
        ShadowPipeline = BuildPipeline(device, library, MetalRenderedCanvas.MakeShadowVertexDescriptor());
        Release(library);

        UnitQuadBuffer = MakeUnitQuadBuffer(device);
        SamplerState = BuildLinearClampSampler(device);
        AtlasTexture = SetupFontAtlasTexture(device, fonts);
    }

    public void UploadAtlasIfDirty(ref int uploadCount)
    {
        if (!Fonts.AtlasDirty) return;
        var rect = Fonts.DirtyRect;
        if (rect.IsEmpty) { Fonts.ClearDirty(); return; }

        var pixels = Fonts.AtlasPixels;
        var region = new MTLRegion
        {
            Origin = new MTLOrigin { X = (nuint)rect.X, Y = (nuint)rect.Y, Z = 0 },
            Size = new MTLSize { Width = (nuint)rect.Width, Height = (nuint)rect.Height, Depth = 1 },
        };
        fixed (byte* basePtr = &MemoryMarshal.GetReference(pixels))
        {
            var atlasW = Fonts.AtlasWidth;
            var srcPtr = basePtr + (rect.Y * atlasW + rect.X);
            msg_Void_MTLRegion_NUInt_IntPtr_NUInt(
                AtlasTexture, Sel("replaceRegion:mipmapLevel:withBytes:bytesPerRow:"),
                region, 0, (IntPtr)srcPtr, (nuint)atlasW);
        }
        Fonts.ClearDirty();
        uploadCount++;
    }

    private static IntPtr LoadLibrary(IntPtr device, string source)
    {
        var nsString = NSString(source);
        IntPtr error = IntPtr.Zero;
        var sel = Sel("newLibraryWithSource:options:error:");
        var library = msg_NewLibrary(device, sel, nsString, IntPtr.Zero, ref error);
        Release(nsString);
        if (library == IntPtr.Zero)
        {
            var msg = error != IntPtr.Zero ? NSStringToManaged(msg_IntPtr(error, Sel("localizedDescription"))) : "(no error)";
            throw new Exception($"newLibraryWithSource failed: {msg}");
        }
        return library;
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr msg_NewLibrary(IntPtr receiver, IntPtr selector, IntPtr source, IntPtr options, ref IntPtr error);

    private static IntPtr NSString(string s)
    {
        var cls = Class("NSString");
        var alloc = msg_IntPtr(cls, Sel("alloc"));
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        fixed (byte* p = bytes)
        {
            return InitWithBytes(alloc, Sel("initWithBytes:length:encoding:"), (IntPtr)p, (nuint)bytes.Length, 4);
        }
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr InitWithBytes(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length, nuint encoding);

    private static string NSStringToManaged(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return "";
        var utf8 = msg_IntPtr(nsString, Sel("UTF8String"));
        return Marshal.PtrToStringUTF8(utf8) ?? "";
    }

    private static IntPtr BuildPipeline(IntPtr device, IntPtr library, IntPtr vertexDescriptor)
    {
        var vsName = NSString("vertexMain");
        var fsName = NSString("fragmentMain");
        var vs = msg_IntPtr(library, Sel("newFunctionWithName:"), vsName);
        var fs = msg_IntPtr(library, Sel("newFunctionWithName:"), fsName);
        Release(vsName);
        Release(fsName);
        if (vs == IntPtr.Zero || fs == IntPtr.Zero)
            throw new Exception("Could not find vertexMain/fragmentMain in compiled library.");

        var descClass = Class("MTLRenderPipelineDescriptor");
        var desc = New(descClass);
        msg_Void_IntPtr(desc, Sel("setVertexFunction:"), vs);
        msg_Void_IntPtr(desc, Sel("setFragmentFunction:"), fs);
        msg_Void_IntPtr(desc, Sel("setVertexDescriptor:"), vertexDescriptor);

        var colorAttachments = msg_IntPtr(desc, Sel("colorAttachments"));
        var color0 = msg_IntPtr_NUInt_NUInt(colorAttachments, Sel("objectAtIndexedSubscript:"), 0, 0);
        msg_Void_UInt(color0, Sel("setPixelFormat:"), (uint)MTLPixelFormat.BGRA8Unorm);
        msg_Void_Bool(color0, Sel("setBlendingEnabled:"), true);
        msg_Void_UInt(color0, Sel("setRgbBlendOperation:"), 0);
        msg_Void_UInt(color0, Sel("setAlphaBlendOperation:"), 0);
        msg_Void_UInt(color0, Sel("setSourceRGBBlendFactor:"), 4);
        msg_Void_UInt(color0, Sel("setSourceAlphaBlendFactor:"), 4);
        msg_Void_UInt(color0, Sel("setDestinationRGBBlendFactor:"), 5);
        msg_Void_UInt(color0, Sel("setDestinationAlphaBlendFactor:"), 5);

        IntPtr error = IntPtr.Zero;
        var pipeline = msg_NewLibrary(device, Sel("newRenderPipelineStateWithDescriptor:error:"), desc, IntPtr.Zero, ref error);
        Release(desc);
        Release(vs);
        Release(fs);
        if (pipeline == IntPtr.Zero)
        {
            var msg = error != IntPtr.Zero ? NSStringToManaged(msg_IntPtr(error, Sel("localizedDescription"))) : "(no error)";
            throw new Exception($"newRenderPipelineStateWithDescriptor failed: {msg}");
        }
        return pipeline;
    }

    private static IntPtr MakeUnitQuadBuffer(IntPtr device)
    {
        Span<float> verts = stackalloc float[12] { 0f, 0f, 1f, 0f, 0f, 1f, 1f, 0f, 1f, 1f, 0f, 1f };
        var byteLen = verts.Length * sizeof(float);
        fixed (float* p = &verts[0])
        {
            return NewBufferWithBytes(device, Sel("newBufferWithBytes:length:options:"),
                (IntPtr)p, (nuint)byteLen, (nuint)MTLResourceOptions.StorageModeShared);
        }
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr NewBufferWithBytes(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length, nuint options);

    private static IntPtr BuildLinearClampSampler(IntPtr device)
    {
        var descClass = Class("MTLSamplerDescriptor");
        var desc = New(descClass);
        msg_Void_UInt(desc, Sel("setMinFilter:"), (uint)MTLSamplerMinMagFilter.Linear);
        msg_Void_UInt(desc, Sel("setMagFilter:"), (uint)MTLSamplerMinMagFilter.Linear);
        msg_Void_UInt(desc, Sel("setSAddressMode:"), (uint)MTLSamplerAddressMode.ClampToEdge);
        msg_Void_UInt(desc, Sel("setTAddressMode:"), (uint)MTLSamplerAddressMode.ClampToEdge);
        var sampler = msg_IntPtr(device, Sel("newSamplerStateWithDescriptor:"), desc);
        Release(desc);
        return sampler;
    }

    private static IntPtr SetupFontAtlasTexture(IntPtr device, FreeTypeFontBackend fonts)
    {
        var width = fonts.AtlasWidth;
        var height = fonts.AtlasHeight;
        var pixels = fonts.AtlasPixels;

        var descClass = Class("MTLTextureDescriptor");
        var desc = TextureDescriptor2D(descClass,
            Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            (nuint)MTLPixelFormat.R8Unorm, (nuint)width, (nuint)height, false);
        msg_Void_UInt(desc, Sel("setStorageMode:"), (uint)MTLStorageMode.Shared);
        msg_Void_UInt(desc, Sel("setUsage:"), (uint)MTLTextureUsage.ShaderRead);

        var tex = msg_IntPtr(device, Sel("newTextureWithDescriptor:"), desc);
        var region = new MTLRegion
        {
            Origin = new MTLOrigin(),
            Size = new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 },
        };
        fixed (byte* p = &MemoryMarshal.GetReference(pixels))
        {
            msg_Void_MTLRegion_NUInt_IntPtr_NUInt(tex,
                Sel("replaceRegion:mipmapLevel:withBytes:bytesPerRow:"),
                region, 0, (IntPtr)p, (nuint)width);
        }
        fonts.ClearDirty();
        return tex;
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr TextureDescriptor2D(
        IntPtr receiver, IntPtr selector,
        nuint pixelFormat, nuint width, nuint height,
        [MarshalAs(UnmanagedType.I1)] bool mipmapped);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Release(AtlasTexture);
        Release(SamplerState);
        Release(UnitQuadBuffer);
        Release(ShadowPipeline);
        Release(ImagePipeline);
        Release(GlyphPipeline);
        Release(RectPipeline);
    }
}
