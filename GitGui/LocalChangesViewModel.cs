namespace GitGui;

internal abstract record LocalChangesViewModel
{
    public sealed record Placeholder(string Text) : LocalChangesViewModel;
    public sealed record Loaded(LocalChangesSnapshot Snapshot) : LocalChangesViewModel;
}