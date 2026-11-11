using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Desktop.Sharing;

/// <summary>
/// The few Windows Runtime calls the share sheet needs, made through COM vtables by hand, as the
/// projection's runtime-generated interop is not available under Native AOT. Slot numbers and IIDs
/// are those of the Windows SDK's headers (<c>windows.applicationmodel.datatransfer.h</c>,
/// <c>windows.storage.h</c>); slots 0-2 are IUnknown's and 3-5 IInspectable's.
/// </summary>
[SupportedOSPlatform("windows")]
internal static unsafe class WinRt
{
    public const int S_OK = 0;
    public const int E_NOINTERFACE = unchecked((int)0x80004002);
    public const int E_FAIL = unchecked((int)0x80004005);
    public const int E_BOUNDS = unchecked((int)0x8000000B);

    public static readonly Guid IidUnknown = new("00000000-0000-0000-C000-000000000046");
    public static readonly Guid IidInspectable = new("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90");
    public static readonly Guid IidAgileObject = new("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");
    public static readonly Guid IidMarshal = new("00000003-0000-0000-C000-000000000046");
    public static readonly Guid IidAgileReference = new("C03F6A43-65A4-9818-987E-E0B810D2A6F2");
    public static readonly Guid IidAsyncInfo = new("00000036-0000-0000-C000-000000000046");

    /// <summary>Calls <c>QueryInterface</c> for <paramref name="iid"/>: the interface, or zero when
    /// <paramref name="unknown"/> does not have it.</summary>
    public static IntPtr Query(IntPtr unknown, in Guid iid)
    {
        var queryInterface = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)Slot(unknown, 0);
        IntPtr result;
        fixed (Guid* pIid = &iid)
        {
            return queryInterface(unknown, pIid, &result) == S_OK ? result : IntPtr.Zero;
        }
    }

    public static uint AddRef(IntPtr unknown) =>
        ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(unknown, 1))(unknown);

    public static void Release(IntPtr unknown)
    {
        if (unknown != IntPtr.Zero)
            ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(unknown, 2))(unknown);
    }

    /// <summary>The function in vtable slot <paramref name="slot"/> of <paramref name="unknown"/>.</summary>
    public static IntPtr Slot(IntPtr unknown, int slot) => (*(IntPtr**)unknown)[slot];

    /// <summary>The activation factory of <paramref name="runtimeClass"/>, as <paramref name="iid"/>.</summary>
    public static IntPtr Factory(string runtimeClass, in Guid iid)
    {
        using var name = new HString(runtimeClass);
        Check(RoGetActivationFactory(name.Handle, in iid, out var factory), $"activating {runtimeClass}");
        return factory;
    }

    /// <summary>
    /// Waits on the calling thread, which must not be one that has to pump messages, for
    /// <paramref name="operation"/>, an <c>IAsyncOperation&lt;T&gt;</c>, and returns its result.
    /// </summary>
    public static IntPtr Await(IntPtr operation, TimeSpan timeout, string what)
    {
        var info = Query(operation, IidAsyncInfo);
        if (info == IntPtr.Zero)
            throw new InvalidOperationException($"{what}: not an async operation");
        try
        {
            var getStatus = (delegate* unmanaged[Stdcall]<IntPtr, int*, int>)Slot(info, 7);
            var getErrorCode = (delegate* unmanaged[Stdcall]<IntPtr, int*, int>)Slot(info, 8);
            var cancel = (delegate* unmanaged[Stdcall]<IntPtr, int>)Slot(info, 9);
            var giveUpAt = DateTime.UtcNow + timeout;
            while (true)
            {
                int status;
                Check(getStatus(info, &status), what);
                // AsyncStatus: Started 0, Completed 1, Canceled 2, Error 3.
                if (status == 1)
                    break;
                if (status == 2)
                    throw new OperationCanceledException($"{what} was cancelled");
                if (status == 3)
                {
                    int error;
                    Check(getErrorCode(info, &error), what);
                    Check(error, what);
                    throw new InvalidOperationException($"{what} failed");
                }
                if (DateTime.UtcNow > giveUpAt)
                {
                    cancel(info);
                    throw new TimeoutException($"{what} took longer than {timeout.TotalSeconds:0} seconds");
                }
                Thread.Sleep(5);
            }
        }
        finally
        {
            Release(info);
        }

        // IAsyncOperation<T>: 6 put_Completed, 7 get_Completed, 8 GetResults.
        var getResults = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Slot(operation, 8);
        IntPtr result;
        Check(getResults(operation, &result), what);
        return result;
    }

    /// <summary>A reference to <paramref name="unknown"/>'s <paramref name="iid"/> that any thread can
    /// resolve (<see cref="Resolve"/>), as an object made on one apartment is not to be called from
    /// another unmarshalled.</summary>
    public static IntPtr AgileReferenceTo(IntPtr unknown, in Guid iid)
    {
        // AGILEREFERENCE_DEFAULT
        Check(RoGetAgileReference(0, in iid, unknown, out var reference), "taking an agile reference");
        return reference;
    }

    /// <summary>Resolves <paramref name="agileReference"/> on the calling thread, as <paramref name="iid"/>.</summary>
    public static IntPtr Resolve(IntPtr agileReference, in Guid iid)
    {
        // IAgileReference: 3 Resolve.
        var resolve = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)Slot(agileReference, 3);
        IntPtr result;
        fixed (Guid* pIid = &iid)
        {
            Check(resolve(agileReference, pIid, &result), "resolving an agile reference");
        }
        return result;
    }

    /// <summary>Throws for a failed <paramref name="hr"/>, naming <paramref name="what"/> failed.</summary>
    public static void Check(int hr, string what)
    {
        if (hr < 0)
            throw new COMException($"{what} failed (0x{hr:X8})", hr);
    }

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int RoGetActivationFactory(IntPtr activatableClassId, in Guid iid, out IntPtr factory);

    [DllImport("combase.dll", PreserveSig = true)]
    private static extern int RoGetAgileReference(int options, in Guid riid, IntPtr pUnk, out IntPtr agileReference);

    [DllImport("combase.dll", PreserveSig = true)]
    public static extern int WindowsCreateString(char* sourceString, uint length, out IntPtr hstring);

    [DllImport("combase.dll", PreserveSig = true)]
    public static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("ole32.dll", PreserveSig = true)]
    public static extern int CoCreateFreeThreadedMarshaler(IntPtr outer, out IntPtr marshaler);
}

/// <summary>A Windows Runtime string, deleted with this. A callee given one copies it if it keeps it.</summary>
[SupportedOSPlatform("windows")]
internal readonly unsafe struct HString : IDisposable
{
    public readonly IntPtr Handle;

    public HString(string value)
    {
        fixed (char* chars = value)
        {
            WinRt.Check(WinRt.WindowsCreateString(chars, (uint)value.Length, out Handle), "making a string");
        }
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            WinRt.WindowsDeleteString(Handle);
    }
}

/// <summary>
/// What a COM object made here does: the interfaces it answers to beyond IUnknown, IAgileObject and
/// IMarshal, and what to let go of once its last reference is released.
/// </summary>
[SupportedOSPlatform("windows")]
internal abstract class ComBehavior
{
    public abstract bool Implements(in Guid iid);

    public virtual void Destroyed() { }
}

/// <summary>
/// Makes COM objects whose behavior is a <see cref="ComBehavior"/>: a block of native memory holding
/// the vtable, the count of references, a handle to the behavior and the free-threaded marshaler
/// aggregated into it, so the object is agile, as C++/WinRT's are, and Windows calls it from any
/// thread without a proxy. The vtables live as long as the process, as they are shared by every
/// object of their kind.
/// </summary>
[SupportedOSPlatform("windows")]
internal static unsafe class ComObjects
{
    private struct Block
    {
        public IntPtr Vtable;
        public int References;
        public IntPtr Behavior;
        public IntPtr Marshaler;
    }

    /// <summary>A delegate's vtable: IUnknown, then <c>Invoke(sender, args)</c>.</summary>
    private static readonly IntPtr HandlerVtable = MakeVtable(
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)&HandlerInvoke);
    /// <summary>An <c>IIterable&lt;T&gt;</c>'s vtable: IUnknown, IInspectable, then <c>First</c>.</summary>
    private static readonly IntPtr IterableVtable = MakeVtable(
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint*, Guid**, int>)&GetIids,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)&GetRuntimeClassName,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, int*, int>)&GetTrustLevel,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)&IterableFirst);

    /// <summary>An <c>IIterator&lt;T&gt;</c>'s vtable: IUnknown, IInspectable, then
    /// <c>get_Current</c>, <c>get_HasCurrent</c>, <c>MoveNext</c> and <c>GetMany</c>.</summary>
    private static readonly IntPtr IteratorVtable = MakeVtable(
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint*, Guid**, int>)&GetIids,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)&GetRuntimeClassName,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, int*, int>)&GetTrustLevel,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)&IteratorCurrent,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)&IteratorHasCurrent,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)&IteratorMoveNext,
        (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, uint*, int>)&IteratorGetMany);

    /// <summary>A delegate, such as an event's <c>TypedEventHandler</c>, of the interface
    /// <paramref name="iid"/>, which runs <paramref name="invoke"/> with the sender and arguments
    /// it is called with, on whatever thread calls it. Holds one reference, the caller's.</summary>
    public static IntPtr Handler(Guid iid, Action<IntPtr, IntPtr> invoke) =>
        Make(HandlerVtable, new DelegateBehavior(iid, invoke));
    /// <summary>An <c>IIterable&lt;T&gt;</c> of the interface <paramref name="iid"/> over the one
    /// item <paramref name="item"/>, whose iterators are of <paramref name="iteratorIid"/>. Holds
    /// one reference, the caller's, and one to the item while it lives.</summary>
    public static IntPtr IterableOfOne(Guid iid, Guid iteratorIid, IntPtr item) =>
        Make(IterableVtable, new IterableOfOne(iid, iteratorIid, item));

    private static IntPtr Make(IntPtr vtable, ComBehavior behavior)
    {
        var block = (Block*)NativeMemory.AllocZeroed((nuint)sizeof(Block));
        block->Vtable = vtable;
        block->References = 1;
        block->Behavior = GCHandle.ToIntPtr(GCHandle.Alloc(behavior));
        var hr = WinRt.CoCreateFreeThreadedMarshaler((IntPtr)block, out block->Marshaler);
        if (hr < 0)
        {
            GCHandle.FromIntPtr(block->Behavior).Free();
            NativeMemory.Free(block);
            WinRt.Check(hr, "making the free-threaded marshaler");
        }
        return (IntPtr)block;
    }

    private static IntPtr MakeVtable(params IntPtr[] methods)
    {
        var vtable = (IntPtr*)NativeMemory.Alloc((nuint)((3 + methods.Length) * sizeof(IntPtr)));
        vtable[0] = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)&QueryInterface;
        vtable[1] = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint>)&AddRef;
        vtable[2] = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint>)&Release;
        for (var i = 0; i < methods.Length; i++)
            vtable[3 + i] = methods[i];
        return (IntPtr)vtable;
    }

    private static ComBehavior BehaviorOf(IntPtr self) =>
        (ComBehavior)GCHandle.FromIntPtr(((Block*)self)->Behavior).Target!;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(IntPtr self, Guid* iid, IntPtr* result)
    {
        var block = (Block*)self;
        if (*iid == WinRt.IidMarshal)
        {
            // The aggregated marshaler's own QueryInterface hands out its IMarshal, whose IUnknown
            // calls come back here.
            var queryInner = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)WinRt.Slot(block->Marshaler, 0);
            return queryInner(block->Marshaler, iid, result);
        }
        var behavior = BehaviorOf(self);
        if (*iid == WinRt.IidUnknown || *iid == WinRt.IidAgileObject || behavior.Implements(*iid)
            || (*iid == WinRt.IidInspectable && behavior is not DelegateBehavior))
        {
            Interlocked.Increment(ref block->References);
            *result = self;
            return WinRt.S_OK;
        }
        *result = IntPtr.Zero;
        return WinRt.E_NOINTERFACE;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(IntPtr self) => (uint)Interlocked.Increment(ref ((Block*)self)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(IntPtr self)
    {
        var block = (Block*)self;
        var left = Interlocked.Decrement(ref block->References);
        if (left != 0)
            return (uint)left;
        var handle = GCHandle.FromIntPtr(block->Behavior);
        ((ComBehavior)handle.Target!).Destroyed();
        handle.Free();
        WinRt.Release(block->Marshaler);
        NativeMemory.Free(block);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetIids(IntPtr self, uint* count, Guid** iids)
    {
        *count = 0;
        *iids = null;
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetRuntimeClassName(IntPtr self, IntPtr* name)
    {
        // The empty string, which is how an object with no runtime class of its own answers.
        *name = IntPtr.Zero;
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetTrustLevel(IntPtr self, int* level)
    {
        // BaseTrust
        *level = 0;
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int HandlerInvoke(IntPtr self, IntPtr sender, IntPtr args)
    {
        try
        {
            ((DelegateBehavior)BehaviorOf(self)).Invoke(sender, args);
            return WinRt.S_OK;
        }
        catch (Exception e)
        {
            // An exception cannot unwind into the Windows code that raised the event: it would end
            // the process. The handler's own work reports its failures; this is the last stop.
            Console.WriteLine($"[FileSharer] An event handler failed: {e}");
            return WinRt.E_FAIL;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IterableFirst(IntPtr self, IntPtr* iterator)
    {
        var iterable = (IterableOfOne)BehaviorOf(self);
        *iterator = Make(IteratorVtable, new IteratorOfOne(iterable.IteratorIid, iterable.Item));
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IteratorCurrent(IntPtr self, IntPtr* item)
    {
        var iterator = (IteratorOfOne)BehaviorOf(self);
        if (iterator.Done)
        {
            *item = IntPtr.Zero;
            return WinRt.E_BOUNDS;
        }
        WinRt.AddRef(iterator.Item);
        *item = iterator.Item;
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IteratorHasCurrent(IntPtr self, byte* hasCurrent)
    {
        *hasCurrent = ((IteratorOfOne)BehaviorOf(self)).Done ? (byte)0 : (byte)1;
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IteratorMoveNext(IntPtr self, byte* hasCurrent)
    {
        var iterator = (IteratorOfOne)BehaviorOf(self);
        iterator.Done = true;
        *hasCurrent = 0;
        return WinRt.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int IteratorGetMany(IntPtr self, uint capacity, IntPtr* items, uint* actual)
    {
        var iterator = (IteratorOfOne)BehaviorOf(self);
        if (iterator.Done || capacity == 0)
        {
            *actual = 0;
            return WinRt.S_OK;
        }
        WinRt.AddRef(iterator.Item);
        items[0] = iterator.Item;
        iterator.Done = true;
        *actual = 1;
        return WinRt.S_OK;
    }
}

[SupportedOSPlatform("windows")]
internal sealed class DelegateBehavior(Guid iid, Action<IntPtr, IntPtr> invoke) : ComBehavior
{
    public override bool Implements(in Guid asked) => asked == iid;

    public void Invoke(IntPtr sender, IntPtr args) => invoke(sender, args);
}

[SupportedOSPlatform("windows")]
internal sealed class IterableOfOne : ComBehavior
{
    private readonly Guid _iid;
    public readonly Guid IteratorIid;
    public readonly IntPtr Item;

    public IterableOfOne(Guid iid, Guid iteratorIid, IntPtr item)
    {
        _iid = iid;
        IteratorIid = iteratorIid;
        Item = item;
        WinRt.AddRef(item);
    }

    public override bool Implements(in Guid asked) => asked == _iid;

    public override void Destroyed() => WinRt.Release(Item);
}

[SupportedOSPlatform("windows")]
internal sealed class IteratorOfOne : ComBehavior
{
    private readonly Guid _iid;
    public readonly IntPtr Item;

    /// <summary>Whether the one item has been passed. Windows walks an iterator from one thread at a
    /// time.</summary>
    public bool Done;

    public IteratorOfOne(Guid iid, IntPtr item)
    {
        _iid = iid;
        Item = item;
        WinRt.AddRef(item);
    }

    public override bool Implements(in Guid asked) => asked == _iid;

    public override void Destroyed() => WinRt.Release(Item);
}
