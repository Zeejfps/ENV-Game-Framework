// CompileCanvasShaders: drives the Slang compiler over a directory of *.slang
// files and emits side-by-side *.gen.glsl and *.gen.metal beside each source.
// Run: dotnet run --project tools/CompileCanvasShaders -- <shaders-dir>

using System.Runtime.InteropServices;
using System.Text;
using SlangIntegrationTest;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: CompileCanvasShaders <shaders-dir> [--glsl-profile glsl_410|glsl_450]");
    return 2;
}

var shadersDir = Path.GetFullPath(args[0]);
if (!Directory.Exists(shadersDir))
{
    Console.Error.WriteLine($"directory not found: {shadersDir}");
    return 2;
}

var glslProfileName = "glsl_410";
for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--glsl-profile" && i + 1 < args.Length)
    {
        glslProfileName = args[++i];
    }
}

var slangFiles = Directory.GetFiles(shadersDir, "*.slang", SearchOption.TopDirectoryOnly);
if (slangFiles.Length == 0)
{
    Console.Error.WriteLine($"no .slang files in {shadersDir}");
    return 0;
}

unsafe
{
    ComPtr<IGlobalSession> globalSessionPtr = new();
    var hr = SlangCompilerAPI.slang_createGlobalSession(0, ref globalSessionPtr.WriteRef());
    if (hr < 0) { Console.Error.WriteLine($"slang_createGlobalSession failed: 0x{hr:X8}"); return 1; }
    var globalSession = globalSessionPtr.Get();

    try
    {
        var glslProfile = globalSession.FindProfile(glslProfileName);
        if (glslProfile == 0)
        {
            Console.Error.WriteLine($"unknown GLSL profile '{glslProfileName}'");
            return 1;
        }

        foreach (var slangPath in slangFiles)
        {
            var moduleName = Path.GetFileNameWithoutExtension(slangPath);
            Console.WriteLine($"compiling {moduleName}.slang");
            if (!Emit(globalSession, shadersDir, moduleName, SlangCompileTarget.SLANG_GLSL,
                    (SlangProfileID)glslProfile, $"{moduleName}.gen.glsl"))
                return 1;
            if (!Emit(globalSession, shadersDir, moduleName, SlangCompileTarget.SLANG_METAL,
                    SlangProfileID.Unknown, $"{moduleName}.gen.metal"))
                return 1;
        }
    }
    finally
    {
        Marshal.Release(globalSessionPtr.Ptr);
        SlangCompilerAPI.slang_shutdown();
    }
}

return 0;

static unsafe bool Emit(IGlobalSession globalSession, string shadersDir, string moduleName,
    SlangCompileTarget target, SlangProfileID profile, string outFileName)
{
    var targetDesc = new TargetDesc { Format = target, Profile = profile };
    var sessionDesc = new SessionDesc();
    sessionDesc.SetTargets(targetDesc);
    sessionDesc.SetSearchPaths(shadersDir);

    var sessionPtr = new ComPtr<ISession>();
    var hr = globalSession.CreateSession(ref sessionDesc, ref sessionPtr.WriteRef());
    if (hr < 0) { Console.Error.WriteLine($"  CreateSession failed: 0x{hr:X8}"); return false; }
    var session = sessionPtr.Get();

    var moduleRaw = IntPtr.Zero;
    var compositeRaw = IntPtr.Zero;
    var linkedRaw = IntPtr.Zero;
    var entryPoints = new List<IntPtr>();

    try
    {
        moduleRaw = session.LoadModule(moduleName, out var loadDiagnostics);
        if (moduleRaw == IntPtr.Zero)
        {
            Console.Error.WriteLine($"  LoadModule failed: {ReadBlob(loadDiagnostics)}");
            return false;
        }

        var module = (IModule)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(moduleRaw, CreateObjectFlags.None);

        // Compile both vertexMain and fragmentMain together.
        foreach (var epName in new[] { "vertexMain", "fragmentMain" })
        {
            hr = module.FindEntryPointByName(epName, out var epRaw);
            if (hr < 0 || epRaw == IntPtr.Zero)
            {
                Console.Error.WriteLine($"  FindEntryPointByName({epName}) failed: 0x{hr:X8}");
                return false;
            }
            entryPoints.Add(epRaw);
        }

        var components = stackalloc IntPtr[1 + entryPoints.Count];
        components[0] = moduleRaw;
        for (var i = 0; i < entryPoints.Count; i++) components[i + 1] = entryPoints[i];
        hr = session.CreateCompositeComponentType((IntPtr)components, entryPoints.Count + 1,
            out compositeRaw, out var compositeDiagnostics);
        if (hr < 0 || compositeRaw == IntPtr.Zero)
        {
            Console.Error.WriteLine($"  CreateCompositeComponentType failed: 0x{hr:X8} {ReadBlob(compositeDiagnostics)}");
            return false;
        }

        var composite = (IComponentType)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(compositeRaw, CreateObjectFlags.None);
        hr = composite.Link(out linkedRaw, out var linkDiagnostics);
        if (hr < 0 || linkedRaw == IntPtr.Zero)
        {
            Console.Error.WriteLine($"  Link failed: 0x{hr:X8} {ReadBlob(linkDiagnostics)}");
            return false;
        }

        var linked = (IComponentType)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(linkedRaw, CreateObjectFlags.None);
        hr = linked.GetTargetCode(0, out var targetCode, out var diag);
        if (hr < 0 || targetCode is null)
        {
            Console.Error.WriteLine($"  GetTargetCode failed: 0x{hr:X8} {ReadBlob(diag)}");
            return false;
        }

        var bytes = ReadBlobBytes(targetCode);
        var outPath = Path.Combine(shadersDir, outFileName);
        File.WriteAllBytes(outPath, bytes);
        Console.WriteLine($"  -> {outFileName} ({bytes.Length} bytes)");
        return true;
    }
    finally
    {
        if (linkedRaw != IntPtr.Zero) Marshal.Release(linkedRaw);
        if (compositeRaw != IntPtr.Zero) Marshal.Release(compositeRaw);
        foreach (var ep in entryPoints) Marshal.Release(ep);
        if (moduleRaw != IntPtr.Zero) Marshal.Release(moduleRaw);
    }
}

static unsafe byte[] ReadBlobBytes(ISlangBlob blob)
{
    var ptr = blob.GetBufferPointer();
    var size = (int)blob.GetBufferSize();
    var bytes = new byte[size];
    new Span<byte>((void*)ptr, size).CopyTo(bytes);
    return bytes;
}

static unsafe string ReadBlob(ISlangBlob? blob)
{
    if (blob is null) return "(no diagnostics)";
    var ptr = blob.GetBufferPointer();
    var size = (int)blob.GetBufferSize();
    return Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)ptr, size));
}
