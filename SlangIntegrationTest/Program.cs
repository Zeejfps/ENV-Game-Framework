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
    
    var createGlobalSessionResult = SlangCompilerAPI.slang_createGlobalSession(0, out var globalSession);
    if (createGlobalSessionResult < 0 || globalSession == null)
        throw new Exception("Failed to create global session");

    try
    {
        Console.WriteLine($"Result: {createGlobalSessionResult}");
        Console.WriteLine(globalSession);

        var profileId = globalSession.FindProfile("glsl_450");
        Console.WriteLine($"Profile Id: {profileId}");

        var targetDesc = new TargetDesc();
        targetDesc.Format = SlangCompileTarget.SLANG_SPIRV;
        targetDesc.Profile = (SlangProfileID)profileId;

        var sessionDesc = new SessionDesc();
        sessionDesc.SetTargets(targetDesc);
        sessionDesc.SetSearchPaths("./Shaders");

        var createSessionResult = globalSession.CreateSession(ref sessionDesc, out var session);
        Console.WriteLine($"Create Session Result: {createSessionResult}");
        if (session == null)
            throw new Exception("Failed to create session");

        Console.WriteLine(session.GetGlobalSession() == globalSession);

        var module = session.LoadModule("hello-world.slang", out var blob);
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

        var entryPointName = "computeMain";
        var findEntryPointByNameResult = module.FindEntryPointByName(entryPointName, out var entryPoint);
        if (findEntryPointByNameResult < 0 || entryPoint == null)
            throw new Exception($"Failed to find entry point with name: {entryPointName}");
    }
    finally
    {
        SlangCompilerAPI.slang_shutdown();
    }
    
    // var searchPaths = new[]
    // {
    //     "./Shaders"
    // };
    //
    // // Allocate an array of pointers
    // IntPtr[] pointerArray = new IntPtr[searchPaths.Length];
    //
    // // Allocate and marshal each string
    // for (int i = 0; i < searchPaths.Length; i++)
    // {
    //     pointerArray[i] = Marshal.StringToHGlobalAnsi(searchPaths[i]);
    // }
    //
    // // Allocate memory for the array of pointers
    // IntPtr result = Marshal.AllocHGlobal(IntPtr.Size * pointerArray.Length);
    // Marshal.Copy(pointerArray, 0, result, pointerArray.Length);
    //
    // var desc = new SlangCompilerAPI.SessionDesc
    // {
    //     SearchPaths = result,
    //     SearchPathCount = searchPaths.Length
    // };
    // var createSessionResult = globalSession.CreateSession(desc, out var session);
    // Console.WriteLine("Create Session worked?");
    // Console.WriteLine(createSessionResult);
    //
    // Console.WriteLine(session.GetGlobalSession() == globalSession);
    //
    // var name = Marshal.StringToHGlobalAnsi("hello-world.slang");
    // var module = session.LoadModule(name, out var blob);
    //
    // if (blob != null)
    // {
    //     //
    //     Console.WriteLine($"Blob size: {blob.GetBufferSize()}");
    //     //
    //     var str = Marshal.PtrToStringAnsi(blob.GetBufferPointer(), blob.GetBufferSize());
    //     Console.WriteLine(str);
    // }

}