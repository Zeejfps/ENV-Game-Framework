using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("0c720e64-8722-4d31-8990-638a98b1c279")]
public partial interface IModule
{
    int FindEntryPointByName(string name, out IntPtr outEntryPoint);

    int GetDefinedEntryPointCount();

    int GetDefinedEntryPoint(int index, out IntPtr outEntryPoint);

    int Serialize(out IntPtr outSerializedBlob);

    int WriteToFile(string fileName);

    IntPtr GetName();

    IntPtr GetFilePath();

    IntPtr GetUniqueIdentity();

    int FindAndCheckEntryPoint(
        string name,
        int stage,
        out IntPtr outEntryPoint,
        out IntPtr outDiagnostics);

    int GetDependencyFileCount();

    IntPtr GetDependencyFilePath(int index);

    IntPtr GetModuleReflection();
}
