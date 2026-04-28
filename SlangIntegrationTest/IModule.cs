using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("0c720e64-8722-4d31-8990-638a98b1c279")]
public partial interface IModule : IComponentType
{
    [PreserveSig] int FindEntryPointByName(string name, out IntPtr outEntryPoint);

    [PreserveSig] int GetDefinedEntryPointCount();

    [PreserveSig] int GetDefinedEntryPoint(int index, out IntPtr outEntryPoint);

    [PreserveSig] int Serialize(out ISlangBlob? outSerializedBlob);

    [PreserveSig] int WriteToFile(string fileName);

    [PreserveSig] IntPtr GetName();

    [PreserveSig] IntPtr GetFilePath();

    [PreserveSig] IntPtr GetUniqueIdentity();

    [PreserveSig] int FindAndCheckEntryPoint(string name, SlangStage stage, out IntPtr outEntryPoint, out ISlangBlob? outDiagnostics);

    [PreserveSig] int GetDependencyFileCount();

    [PreserveSig] IntPtr GetDependencyFilePath(int index);

    [PreserveSig] IntPtr GetModuleReflection();

    [PreserveSig] int Disassemble(out ISlangBlob? outDisassembledBlob);
}
