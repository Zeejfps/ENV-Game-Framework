namespace ZGF.Fonts;

public readonly record struct FontHandle(int Id)
{
    public bool IsValid => Id > 0;
}
