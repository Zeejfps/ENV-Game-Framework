namespace ZGF.Gui.Immediate;

/// <summary>The two files a <c>Look</c> wrote, as absolute paths.</summary>
public sealed record LookFiles(string Text, string Png)
{
    public override string ToString() => $"{Text}\n{Png}";
}
