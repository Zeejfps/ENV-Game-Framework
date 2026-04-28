using System.Runtime.InteropServices;
using System.Text;
using SlangIntegrationTest;

unsafe
{
    ComPtr<IGlobalSession> globalSessionPtr = new();
    var hr = SlangCompilerAPI.slang_createGlobalSession(0, ref globalSessionPtr.WriteRef());
    if (hr < 0) throw new Exception($"slang_createGlobalSession failed: 0x{hr:X8}");
    var globalSession = globalSessionPtr.Get();

    var moduleRaw = IntPtr.Zero;
    var vertexEpRaw = IntPtr.Zero;
    var fragmentEpRaw = IntPtr.Zero;
    var compositeRaw = IntPtr.Zero;
    var linkedRaw = IntPtr.Zero;

    try
    {
        var profileId = globalSession.FindProfile("glsl_450");
        Console.WriteLine($"Profile: glsl_450 -> {profileId}");

        var targetDesc = new TargetDesc
        {
            Format = SlangCompileTarget.SLANG_SPIRV,
            Profile = (SlangProfileID)profileId,
        };

        var sessionDesc = new SessionDesc();
        sessionDesc.SetTargets(targetDesc);
        sessionDesc.SetSearchPaths("./Shaders");

        var sessionPtr = new ComPtr<ISession>();
        hr = globalSession.CreateSession(ref sessionDesc, ref sessionPtr.WriteRef());
        if (hr < 0) throw new Exception($"CreateSession failed: 0x{hr:X8}");
        var session = sessionPtr.Get();

        moduleRaw = session.LoadModule("hello-world", out var loadDiagnostics);
        if (moduleRaw == IntPtr.Zero) throw new Exception($"LoadModule failed: {ReadBlob(loadDiagnostics)}");
        Console.WriteLine($"Loaded module, total loaded: {session.GetLoadedModuleCount()}");

        var module = (IModule)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(moduleRaw, CreateObjectFlags.None);

        hr = module.FindEntryPointByName("vertexMain", out vertexEpRaw);
        if (hr < 0 || vertexEpRaw == IntPtr.Zero) throw new Exception($"FindEntryPointByName(vertexMain) failed: 0x{hr:X8}");

        hr = module.FindEntryPointByName("fragmentMain", out fragmentEpRaw);
        if (hr < 0 || fragmentEpRaw == IntPtr.Zero) throw new Exception($"FindEntryPointByName(fragmentMain) failed: 0x{hr:X8}");

        // Composite [module, vertexEP, fragmentEP] so we can link as one program.
        var components = stackalloc IntPtr[3] { moduleRaw, vertexEpRaw, fragmentEpRaw };
        hr = session.CreateCompositeComponentType((IntPtr)components, 3, out compositeRaw, out var compositeDiagnostics);
        if (hr < 0 || compositeRaw == IntPtr.Zero)
            throw new Exception($"CreateCompositeComponentType failed: 0x{hr:X8} {ReadBlob(compositeDiagnostics)}");

        var composite = (IComponentType)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(compositeRaw, CreateObjectFlags.None);

        hr = composite.Link(out linkedRaw, out var linkDiagnostics);
        if (hr < 0 || linkedRaw == IntPtr.Zero) throw new Exception($"Link failed: 0x{hr:X8} {ReadBlob(linkDiagnostics)}");

        var linked = (IComponentType)SlangCompilerAPI.ComWrappers.GetOrCreateObjectForComInstance(linkedRaw, CreateObjectFlags.None);

        hr = linked.GetEntryPointCode(0, 0, out var vertexCode, out var vertexCodeDiagnostics);
        if (hr < 0 || vertexCode is null) throw new Exception($"GetEntryPointCode(vertex) failed: 0x{hr:X8} {ReadBlob(vertexCodeDiagnostics)}");

        hr = linked.GetEntryPointCode(1, 0, out var fragmentCode, out var fragmentCodeDiagnostics);
        if (hr < 0 || fragmentCode is null) throw new Exception($"GetEntryPointCode(fragment) failed: 0x{hr:X8} {ReadBlob(fragmentCodeDiagnostics)}");

        var vertexBytes = ReadBlobBytes(vertexCode);
        var fragmentBytes = ReadBlobBytes(fragmentCode);
        Console.WriteLine($"vertexMain SPIR-V: {vertexBytes.Length} bytes, magic 0x{BitConverter.ToUInt32(vertexBytes, 0):X8}");
        Console.WriteLine($"fragmentMain SPIR-V: {fragmentBytes.Length} bytes, magic 0x{BitConverter.ToUInt32(fragmentBytes, 0):X8}");

        // Whole-program target code blob.
        hr = linked.GetTargetCode(0, out var programCode, out var programCodeDiagnostics);
        if (hr < 0 || programCode is null) throw new Exception($"GetTargetCode failed: 0x{hr:X8} {ReadBlob(programCodeDiagnostics)}");
        Console.WriteLine($"Whole-program SPIR-V: {ReadBlobBytes(programCode).Length} bytes");

        // Reflection — must run before Marshal.Release(linkedRaw) below since reflection
        // handles are non-owning views into the linked program's memory.
        var layoutPtr = linked.GetLayout(0, out _);
        if (layoutPtr == IntPtr.Zero) throw new Exception("GetLayout returned null");
        var layout = new ProgramLayout(layoutPtr);

        Console.WriteLine();
        Console.WriteLine($"=== Reflection: {layout.ParameterCount} global parameter(s), {layout.EntryPointCount} entry point(s) ===");

        for (uint i = 0; i < layout.ParameterCount; i++)
        {
            var p = layout.GetParameter(i);
            var v = p.Variable;
            var t = v.Type;
            var category = p.TypeLayout.ParameterCategory;
            var space = p.GetSpace(category);
            var index = p.GetOffset(category);
            var extra = t.Kind switch
            {
                SlangTypeKind.Resource => $" shape={t.ResourceShape.BaseShape()} access={t.ResourceAccess}",
                SlangTypeKind.ConstantBuffer or SlangTypeKind.ParameterBlock => $" element={t.ElementType.Kind}",
                _ => "",
            };
            Console.WriteLine($"  [{i}] {v.Name} : {t.Kind}{extra} [category={category} space={space} index={index}]");
        }

        for (ulong i = 0; i < layout.EntryPointCount; i++)
        {
            var ep = layout.GetEntryPointByIndex(i);
            var line = $"  entry: {ep.Name} stage={ep.Stage}";
            if (ep.Stage == SlangStage.Compute)
            {
                var (gx, gy, gz) = ep.GetComputeThreadGroupSize();
                line += $" threadGroup={gx}x{gy}x{gz}";
            }
            Console.WriteLine(line);
        }

        // Descriptor-set introspection on the global params type layout.
        var globals = layout.GlobalParamsTypeLayout;
        Console.WriteLine();
        Console.WriteLine($"=== Descriptor sets: {globals.DescriptorSetCount}, binding ranges: {globals.BindingRangeCount}, sub-object ranges: {globals.SubObjectRangeCount} ===");
        for (long s = 0; s < globals.DescriptorSetCount; s++)
        {
            var rangeCount = globals.GetDescriptorSetDescriptorRangeCount(s);
            Console.WriteLine($"  set #{s}: spaceOffset={globals.GetDescriptorSetSpaceOffset(s)}, ranges={rangeCount}");
            for (long r = 0; r < rangeCount; r++)
            {
                var t = globals.GetDescriptorSetDescriptorRangeType(s, r);
                var c = globals.GetDescriptorSetDescriptorRangeCategory(s, r);
                var idx = globals.GetDescriptorSetDescriptorRangeIndexOffset(s, r);
                var cnt = globals.GetDescriptorSetDescriptorRangeDescriptorCount(s, r);
                Console.WriteLine($"    range #{r}: type={t} category={c} index={idx} count={cnt}");
            }
        }
        for (long b = 0; b < globals.BindingRangeCount; b++)
        {
            var t = globals.GetBindingRangeType(b);
            var setIdx = globals.GetBindingRangeDescriptorSetIndex(b);
            var firstRange = globals.GetBindingRangeFirstDescriptorRangeIndex(b);
            var rangeCnt = globals.GetBindingRangeDescriptorRangeCount(b);
            Console.WriteLine($"  bindingRange #{b}: type={t} set={setIdx} firstRange={firstRange} rangeCount={rangeCnt}");
        }

        // Modifiers and user attributes on each global parameter (likely empty for hello-world).
        for (uint i = 0; i < layout.ParameterCount; i++)
        {
            var v = layout.GetParameter(i).Variable;
            var modifierFlags = new List<string>();
            foreach (var id in (SlangModifierID[])Enum.GetValues(typeof(SlangModifierID)))
                if (v.HasModifier(id)) modifierFlags.Add(id.ToString());
            var attrCount = v.UserAttributeCount;
            if (modifierFlags.Count > 0 || attrCount > 0)
            {
                var attrs = string.Join(", ", Enumerable.Range(0, (int)attrCount)
                    .Select(j => v.GetUserAttribute((uint)j).Name ?? "<unnamed>"));
                Console.WriteLine($"  param {v.Name}: modifiers=[{string.Join(",", modifierFlags)}] attrs=[{attrs}]");
            }
        }
    }
    finally
    {
        if (linkedRaw != IntPtr.Zero) Marshal.Release(linkedRaw);
        if (compositeRaw != IntPtr.Zero) Marshal.Release(compositeRaw);
        if (fragmentEpRaw != IntPtr.Zero) Marshal.Release(fragmentEpRaw);
        if (vertexEpRaw != IntPtr.Zero) Marshal.Release(vertexEpRaw);
        if (moduleRaw != IntPtr.Zero) Marshal.Release(moduleRaw);
        Marshal.Release(globalSessionPtr.Ptr);
        SlangCompilerAPI.slang_shutdown();
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
