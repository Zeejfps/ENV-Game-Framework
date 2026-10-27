using System.Text;
using static ZGF.Gui.Hosting.Xcb;

namespace ZGF.Gui.Hosting;

/// <summary>The atoms the clipboard speaks in, interned once per connection.</summary>
internal sealed unsafe class X11Atoms
{
    // Where conversions the clipboard asks for are delivered, on its own window.
    private const string ReceivingPropertyName = "ZGF_CLIPBOARD";

    private static readonly string[] Names =
    [
        "CLIPBOARD", "CLIPBOARD_MANAGER", "TARGETS", "MULTIPLE", "SAVE_TARGETS", "INCR", "ATOM_PAIR", "NULL",
        "UTF8_STRING", "TEXT", "text/plain;charset=utf-8", "text/html", ReceivingPropertyName,
    ];

    private X11Atoms(uint[] atoms)
    {
        Clipboard = atoms[0];
        ClipboardManager = atoms[1];
        Targets = atoms[2];
        Multiple = atoms[3];
        SaveTargets = atoms[4];
        Incr = atoms[5];
        AtomPair = atoms[6];
        Null = atoms[7];
        Utf8String = atoms[8];
        Text = atoms[9];
        PlainTextUtf8 = atoms[10];
        Html = atoms[11];
        ReceivingProperty = atoms[12];
    }

    public uint Clipboard { get; }
    public uint ClipboardManager { get; }
    public uint Targets { get; }
    public uint Multiple { get; }
    public uint SaveTargets { get; }
    public uint Incr { get; }
    public uint AtomPair { get; }
    public uint Null { get; }
    public uint Utf8String { get; }
    public uint Text { get; }
    public uint PlainTextUtf8 { get; }
    public uint Html { get; }
    public uint ReceivingProperty { get; }

    /// <summary>Interns every atom, sending all the requests before waiting on the first reply.</summary>
    public static X11Atoms Intern(IntPtr connection)
    {
        var cookies = Names
            .Select(name => SendInternAtom(connection, name))
            .ToArray();
        var atoms = cookies
            .Select(cookie => ReceiveAtom(connection, cookie))
            .ToArray();
        return new X11Atoms(atoms);
    }

    private static uint SendInternAtom(IntPtr connection, string name)
    {
        var bytes = Encoding.ASCII.GetBytes(name);
        fixed (byte* pointer = bytes)
            return xcb_intern_atom(connection, 0, (ushort)bytes.Length, pointer);
    }

    private static uint ReceiveAtom(IntPtr connection, uint cookie)
    {
        var reply = xcb_intern_atom_reply(connection, cookie, null);
        if (reply == null)
            return None;
        var atom = reply->Atom;
        Libc.free(reply);
        return atom;
    }
}
