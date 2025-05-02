public sealed class LinksRepo
{
    private readonly HashSet<Link> _links = new();

    public IEnumerable<Link> GetAll()
    {
        return _links;
    }
    
    public void Add(Link link)
    {
        _links.Add(link);
    }

    public void Remove(Link newLink)
    {
        _links.Remove(newLink);
    }
}