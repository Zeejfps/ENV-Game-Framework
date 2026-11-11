using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Desktop.Sharing;

/// <summary>
/// Windows' share sheet, as a desktop app shows it: the window's <c>DataTransferManager</c>, through
/// <c>IDataTransferManagerInterop</c>, asks for the data as the sheet opens (<c>DataRequested</c>),
/// and is handed the file as a <c>StorageFile</c>.
/// </summary>
/// <remarks>
/// The sheet says how it ended through the package it was handed: <c>ShareCompleted</c> once an app
/// took it, <c>ShareCanceled</c> (Windows 11) once it was closed without one, and on Windows 10,
/// which lacks the second, the package being destroyed as the sheet lets go of it, which counts as
/// shared only once the user picked an app (<c>TargetApplicationChosen</c>).
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsFileSharer : IFileSharer
{
    private const string DataTransferManagerClass = "Windows.ApplicationModel.DataTransfer.DataTransferManager";
    private const string StorageFileClass = "Windows.Storage.StorageFile";

    private static readonly Guid IidDataTransferManagerInterop = new("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8");
    private static readonly Guid IidDataTransferManager = new("a5caee9b-8708-49d1-8d36-67d25a8da00c");
    private static readonly Guid IidDataPackage3 = new("88f31f5d-787b-4d32-965a-a9838105a056");
    private static readonly Guid IidDataPackage4 = new("13a24ec8-9382-536f-852a-3045e1b29a3b");
    private static readonly Guid IidStorageFileStatics = new("5984c710-daf2-43c8-8bb4-a4d3eacfd03f");
    private static readonly Guid IidStorageItem = new("4207a996-ca2f-42f7-bde8-8b10457a7f30");
    // IIterable<IStorageItem> and IIterator<IStorageItem>
    private static readonly Guid IidStorageItems = new("bb8b8418-65d1-544b-b083-6d172f568c73");
    private static readonly Guid IidStorageItemIterator = new("05b487c2-3830-5d3c-98da-25fa11542dbd");
    // TypedEventHandler<DataTransferManager, DataRequestedEventArgs>
    private static readonly Guid IidDataRequestedHandler = new("ec6f9cc8-46d0-5e0e-b4d2-7d7773ae37a0");
    // TypedEventHandler<DataTransferManager, TargetApplicationChosenEventArgs>
    private static readonly Guid IidTargetChosenHandler = new("c4ac1ba2-7851-5a44-bc8d-3d7c713f1f41");
    // TypedEventHandler<DataPackage, ShareCompletedEventArgs>
    private static readonly Guid IidShareCompletedHandler = new("f8f7e24a-56fe-58df-bc15-2365aec03966");
    // TypedEventHandler<DataPackage, IInspectable>: ShareCanceled's and Destroyed's
    private static readonly Guid IidPackageHandler = new("c156b0c3-1cbc-5ca4-901c-62c5a8ca5cb5");

    private static readonly TimeSpan OpenFileTimeout = TimeSpan.FromSeconds(10);

    private readonly IntPtr _window;
    private readonly Action<Action> _post;

    /// <summary>The share whose sheet has been asked for and has not asked for its data yet, which a
    /// newer one takes the place of. UI thread only.</summary>
    private Sharing? _waiting;

    public WindowsFileSharer(IntPtr window, Action<Action> post)
    {
        _window = window;
        _post = post;
    }

    /// <summary>Whether this Windows has the share sheet for desktop apps, which Windows 10 brought.</summary>
    public static bool IsAvailable()
    {
        try
        {
            var interop = WinRt.Factory(DataTransferManagerClass, IidDataTransferManagerInterop);
            WinRt.Release(interop);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public bool CanShare => true;

    public void Share(FileToShare file, Action<ShareOutcome> onDone)
    {
        var share = new Sharing(file, onDone, _post);
        // The file is opened as a StorageFile away from the UI thread, as that waits on the storage
        // broker, then handed back to it through an agile reference.
        var opener = new Thread(() =>
        {
            IntPtr item;
            try
            {
                item = OpenAsStorageItem(file.Path);
            }
            catch (Exception e) when (IsOpenFailure(e))
            {
                share.Finish(new ShareOutcome.Failed($"Could not open {file.Path} to share: {e.Message}"));
                return;
            }
            _post(() => Show(share, item));
        })
        {
            IsBackground = true,
            Name = "Share sheet file",
        };
        opener.SetApartmentState(ApartmentState.MTA);
        opener.Start();
    }

    private static bool IsOpenFailure(Exception e) =>
        e is COMException or TimeoutException or OperationCanceledException or InvalidOperationException or IOException
            or UnauthorizedAccessException;

    /// <summary>An agile reference to <paramref name="path"/> as an <c>IStorageItem</c>.</summary>
    private static IntPtr OpenAsStorageItem(string path)
    {
        var file = OpenStorageFile(path);
        var item = IntPtr.Zero;
        try
        {
            item = WinRt.Query(file, IidStorageItem);
            if (item == IntPtr.Zero)
                throw new InvalidOperationException("A StorageFile that is not an IStorageItem");
            return WinRt.AgileReferenceTo(item, IidStorageItem);
        }
        finally
        {
            WinRt.Release(item);
            WinRt.Release(file);
        }
    }

    /// <summary><paramref name="path"/> as an <c>IStorageFile</c>, waited for on the calling thread.</summary>
    private static IntPtr OpenStorageFile(string path)
    {
        var statics = WinRt.Factory(StorageFileClass, IidStorageFileStatics);
        var operation = IntPtr.Zero;
        try
        {
            // IStorageFileStatics: 6 GetFileFromPathAsync, whose result is an IStorageFile.
            var getFileFromPath = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)WinRt.Slot(statics, 6);
            using (var name = new HString(path))
            {
                WinRt.Check(getFileFromPath(statics, name.Handle, &operation), "opening the file");
            }
            return WinRt.Await(operation, OpenFileTimeout, "opening the file");
        }
        finally
        {
            WinRt.Release(operation);
            WinRt.Release(statics);
        }
    }

    /// <summary>Opens the sheet for <paramref name="share"/>, whose file is <paramref name="item"/>.
    /// UI thread.</summary>
    private void Show(Sharing share, IntPtr item)
    {
        share.Item = item;
        _waiting?.Finish(new ShareOutcome.Failed("Another share sheet was asked for first"));
        IntPtr interop = IntPtr.Zero;
        try
        {
            interop = WinRt.Factory(DataTransferManagerClass, IidDataTransferManagerInterop);
            // IDataTransferManagerInterop: 3 GetForWindow, 4 ShowShareUIForWindow.
            var getForWindow = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)WinRt.Slot(interop, 3);
            var showShareUi = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)WinRt.Slot(interop, 4);
            IntPtr manager;
            var iid = IidDataTransferManager;
            WinRt.Check(getForWindow(interop, _window, &iid, &manager), "finding the window's share sheet");
            share.Manager = manager;

            share.DataRequested = Subscribe(manager, 6, IidDataRequestedHandler, (_, args) => OnDataRequested(share, args));
            share.TargetChosen = Subscribe(manager, 8, IidTargetChosenHandler, (_, _) => share.TargetWasChosen());
            _waiting = share;
            WinRt.Check(showShareUi(interop, _window), "showing the share sheet");
        }
        catch (COMException e)
        {
            share.Finish(new ShareOutcome.Failed(e.Message));
        }
        finally
        {
            WinRt.Release(interop);
        }
    }

    /// <summary>The sheet asks for the data as it opens; it is given the file, its title and words. The
    /// sheet's thread, which is the window's.</summary>
    private void OnDataRequested(Sharing share, IntPtr args)
    {
        if (_waiting == share)
            _waiting = null;
        if (!share.TakeRequest())
            return;
        IntPtr request = IntPtr.Zero, package = IntPtr.Zero;
        try
        {
            // IDataRequestedEventArgs: 6 get_Request.
            var getRequest = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)WinRt.Slot(args, 6);
            WinRt.Check(getRequest(args, &request), "reading the share request");
            // IDataRequest: 6 get_Data.
            var getData = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)WinRt.Slot(request, 6);
            WinRt.Check(getData(request, &package), "reading the share's package");

            Fill(package, share);
            Watch(package, share);
        }
        catch (COMException e)
        {
            if (request != IntPtr.Zero)
                FailRequest(request, e.Message);
            share.Finish(new ShareOutcome.Failed(e.Message));
        }
        finally
        {
            WinRt.Release(package);
            WinRt.Release(request);
        }
    }

    private static void Fill(IntPtr package, Sharing share)
    {
        // IDataPackage: 7 get_Properties, 16 SetText, 22 SetStorageItemsReadOnly.
        var getProperties = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)WinRt.Slot(package, 7);
        var setText = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)WinRt.Slot(package, 16);
        var setStorageItems = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)WinRt.Slot(package, 22);

        IntPtr properties;
        WinRt.Check(getProperties(package, &properties), "reading the share's properties");
        try
        {
            // IDataPackagePropertySet: 7 put_Title, 9 put_Description. The sheet refuses a package
            // with no title.
            var putTitle = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)WinRt.Slot(properties, 7);
            var putDescription = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)WinRt.Slot(properties, 9);
            using (var title = new HString(share.File.Title))
            {
                WinRt.Check(putTitle(properties, title.Handle), "titling the share");
            }
            if (!string.IsNullOrEmpty(share.File.Text))
            {
                using var description = new HString(share.File.Text);
                WinRt.Check(putDescription(properties, description.Handle), "describing the share");
            }
        }
        finally
        {
            WinRt.Release(properties);
        }

        if (!string.IsNullOrEmpty(share.File.Text))
        {
            using var text = new HString(share.File.Text);
            WinRt.Check(setText(package, text.Handle), "adding the share's words");
        }

        var item = WinRt.Resolve(share.Item, IidStorageItem);
        var items = IntPtr.Zero;
        try
        {
            items = ComObjects.IterableOfOne(IidStorageItems, IidStorageItemIterator, item);
            WinRt.Check(setStorageItems(package, items), "adding the file to the share");
        }
        finally
        {
            WinRt.Release(items);
            WinRt.Release(item);
        }
    }

    /// <summary>Listens for how the sheet ends, on <paramref name="package"/>, which the sheet holds
    /// and lets go of once it is done with it.</summary>
    private static void Watch(IntPtr package, Sharing share)
    {
        // IDataPackage3: 6 add_ShareCompleted. Windows 10 1709 on.
        var package3 = WinRt.Query(package, IidDataPackage3);
        if (package3 != IntPtr.Zero)
        {
            Subscribe(package3, 6, IidShareCompletedHandler, (_, _) => share.Finish(new ShareOutcome.Shared()));
            WinRt.Release(package3);
        }
        // IDataPackage4: 6 add_ShareCanceled. Windows 11 on.
        var package4 = WinRt.Query(package, IidDataPackage4);
        if (package4 != IntPtr.Zero)
        {
            Subscribe(package4, 6, IidPackageHandler, (_, _) => share.Finish(new ShareOutcome.Cancelled()));
            WinRt.Release(package4);
        }
        // IDataPackage: 12 add_Destroyed.
        Subscribe(package, 12, IidPackageHandler, (_, _) => share.Finish(share.WasTargetChosen
            ? new ShareOutcome.Shared()
            : new ShareOutcome.Cancelled()));
    }

    private static void FailRequest(IntPtr request, string reason)
    {
        // IDataRequest: 9 FailWithDisplayText.
        var fail = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)WinRt.Slot(request, 9);
        using var text = new HString(reason);
        fail(request, text.Handle);
    }

    /// <summary>Adds a handler of <paramref name="handlerIid"/> running <paramref name="invoke"/> to
    /// the event whose <c>add_</c> is in <paramref name="addSlot"/> of <paramref name="source"/>, its
    /// <c>remove_</c> being the slot after. The registration's token.</summary>
    private static long Subscribe(IntPtr source, int addSlot, Guid handlerIid, Action<IntPtr, IntPtr> invoke)
    {
        var add = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, long*, int>)WinRt.Slot(source, addSlot);
        var handler = ComObjects.Handler(handlerIid, invoke);
        try
        {
            long token;
            WinRt.Check(add(source, handler, &token), "listening to the share sheet");
            return token;
        }
        finally
        {
            // The event source holds its own reference while subscribed.
            WinRt.Release(handler);
        }
    }

    private static void Unsubscribe(IntPtr source, int removeSlot, long token)
    {
        var remove = (delegate* unmanaged[Stdcall]<IntPtr, long, int>)WinRt.Slot(source, removeSlot);
        remove(source, token);
    }

    /// <summary>One file being shared: what the sheet has said so far, and what to let go of once it
    /// ends.</summary>
    private sealed class Sharing(FileToShare file, Action<ShareOutcome> onDone, Action<Action> post)
    {
        public readonly FileToShare File = file;

        /// <summary>The agile reference to the file as an <c>IStorageItem</c>; UI thread.</summary>
        public IntPtr Item;

        /// <summary>The window's <c>DataTransferManager</c>; UI thread.</summary>
        public IntPtr Manager;

        public long? DataRequested;
        public long? TargetChosen;

        private int _requested;
        private int _targetChosen;
        private int _finished;

        public bool WasTargetChosen => Volatile.Read(ref _targetChosen) == 1;

        public void TargetWasChosen() => Interlocked.Exchange(ref _targetChosen, 1);

        /// <summary>Whether this is the first time the sheet asks for the data, as it asks once.</summary>
        public bool TakeRequest() => Interlocked.Exchange(ref _requested, 1) == 0;

        /// <summary>Ends the share with <paramref name="outcome"/> unless it has ended: on the UI
        /// thread, lets go of the window's manager and what it listened to there, then tells the
        /// caller. From any thread.</summary>
        public void Finish(ShareOutcome outcome)
        {
            if (Interlocked.Exchange(ref _finished, 1) != 0)
                return;
            post(() =>
            {
                if (Manager != IntPtr.Zero)
                {
                    // IDataTransferManager: 7 remove_DataRequested, 9 remove_TargetApplicationChosen.
                    // The manager is the window's for as long as it lives, so a handler left on it
                    // would answer the next share too.
                    if (DataRequested != null)
                        Unsubscribe(Manager, 7, DataRequested.Value);
                    if (TargetChosen != null)
                        Unsubscribe(Manager, 9, TargetChosen.Value);
                    WinRt.Release(Manager);
                    Manager = IntPtr.Zero;
                }
                WinRt.Release(Item);
                Item = IntPtr.Zero;
                onDone(outcome);
            });
        }
    }
}
