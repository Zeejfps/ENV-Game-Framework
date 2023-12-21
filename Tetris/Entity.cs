namespace Tetris;

public struct Entity
{
    private readonly HashSet<string> m_Tags = new();

    public Entity() {}

    public void AddTag(string tag)
    {
        m_Tags.Add(tag);
    }

    public void RemoveTag(string tag)
    {
        m_Tags.Remove(tag);
    }

    public bool WithTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (!m_Tags.Contains(tag))
            {
                return false;
            }
        }
        return true;
    }

    public bool WithoutTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (m_Tags.Contains(tag))
            {
                return false;
            }
        }

        return true;
    }
}