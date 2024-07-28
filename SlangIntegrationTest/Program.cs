// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using System.Text;
using SlangIntegrationTest;

unsafe
{
    var entryPointInterfaceGuid = new Guid(
        0x8f241361, 0xf5bd, 0x4ca0, 0xa3, 0xac, 0x2, 0xf7, 0xfa, 0x24, 0x2, 0xb8 
    );
    Console.WriteLine($"Module Guid: {entryPointInterfaceGuid}");

    var sessionInterfaceGuid = new Guid("67618701-d116-468f-ab3b-474bedce0e3d");
    
    ComPtr<IGlobalSession> globalSessionPtr = new();
    var createGlobalSessionResult = SlangCompilerAPI.slang_createGlobalSession(0, ref globalSessionPtr.WriteRef());
    Console.WriteLine($"Result: {createGlobalSessionResult}");
    if (createGlobalSessionResult < 0 )
        throw new Exception("Failed to create global session");

    var globalSession = globalSessionPtr.Get();
    try
    {
        var profileId = globalSession.FindProfile("glsl_450");
        Console.WriteLine($"Profile Id: {profileId}");
        
        var targetDesc = new TargetDesc();
        targetDesc.Format = SlangCompileTarget.SLANG_SPIRV;
        targetDesc.Profile = (SlangProfileID)profileId;
        
        var sessionDesc = new SessionDesc();
        sessionDesc.SetTargets(targetDesc);
        sessionDesc.SetSearchPaths("./Shaders");

        var sessionPtr = new ComPtr<ISession>();
        var createSessionResult = globalSession.CreateSession(ref sessionDesc, ref sessionPtr.WriteRef());
        Console.WriteLine($"Create Session Result: {createSessionResult}");
        if (createSessionResult < 0)
            throw new Exception($"Failed to create session: {createSessionResult}");

        Marshal.QueryInterface(sessionPtr.Ptr, ref sessionInterfaceGuid, out var test);
        var count = Marshal.AddRef(sessionPtr.Ptr);
        Console.WriteLine($"Session Ptr: {sessionPtr} and Test: {test}, Count: {count}");
        
        var session = sessionPtr.Get();
        var globalSessionPtrNew = session.GetGlobalSession();
        Console.WriteLine(globalSessionPtrNew + $", {globalSessionPtr}");
        Console.WriteLine(session.GetGlobalSession() == globalSession);
        
        var loadedModuleCount = session.GetLoadedModuleCount();
        Console.WriteLine($"Loaded Module Count: {loadedModuleCount}");
        
        var modulePtr = session.LoadModule("hello-world", out var blob);
        Console.WriteLine(modulePtr);
        
        var refCount = Marshal.AddRef(modulePtr);
        Console.WriteLine($"Module Ref Count {refCount}");
        
        var module = (IModule)Marshal.GetObjectForIUnknown(modulePtr);

        if (module == null)
        {
            var error = "Unknown Error";
            if (blob != null)
            {
                var data = new Span<byte>((void*)blob.GetBufferPointer(), blob.GetBufferSize().ToInt32());
                error = Encoding.ASCII.GetString(data);
            }
            
            throw new Exception($"Error loading module: {error}");
        }
        
        loadedModuleCount = session.GetLoadedModuleCount();
        Console.WriteLine($"Loaded Module Count: {loadedModuleCount}");
        
        var findEntryPointByNameResult = module.FindEntryPointByName("vertexMain", out var vertexShaderEntryPoint);
        Console.WriteLine($"Find Entry Point Result: {findEntryPointByNameResult}");
        if (findEntryPointByNameResult < 0)
            throw new Exception($"Failed to find entry point with name: {findEntryPointByNameResult}");
        
        findEntryPointByNameResult = module.FindEntryPointByName("fragmentMain", out var fragmentShaderEntryPoint);
        Console.WriteLine($"Find Entry Point Result: {findEntryPointByNameResult}");
        if (findEntryPointByNameResult < 0)
            throw new Exception($"Failed to find entry point with name: {findEntryPointByNameResult}");
        
        var ptr = Marshal.GetComInterfaceForObject<IModule, IComponentType>(module);
        Console.WriteLine($"M: {modulePtr} , O {ptr}");
        var component = (IComponentType)Marshal.GetObjectForIUnknown(ptr);
        var layoutPtr = component.GetLayout(0, out var blobPtr);
        Console.WriteLine($"Laout Ptr: {layoutPtr}");

        Marshal.Release(modulePtr);
    }
    finally
    {
        Marshal.Release(globalSessionPtr.Ptr);
        SlangCompilerAPI.slang_shutdown();
    }
}