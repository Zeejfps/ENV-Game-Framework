// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using SlangIntegrationTest;

unsafe
{
    var createGlobalSessionResult = SlangCompilerAPI.slang_createGlobalSession(0, out var globalSession);
    Console.WriteLine($"Result: {createGlobalSessionResult}");
    Console.WriteLine(globalSession);

    var profileId = globalSession.FindProfile("glsl_450");
    Console.WriteLine($"Profile Id: {profileId}");

    var sessionDescription = new SessionDesc();
    var createSessionResult = globalSession.CreateSession(ref sessionDescription, out var session);
    Console.WriteLine($"Create Session Result: {createSessionResult}");

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