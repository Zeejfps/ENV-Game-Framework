public sealed class LinksRepo
{
    private readonly List<Link> _links = new();
    
    public IEnumerable<Link> GetAll()
    {
        return _links;
    }
    
    public void Add(Link link)
    {
        _links.Add(link);
    }
}