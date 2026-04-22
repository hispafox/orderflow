namespace Products.API.Domain;

public class Category
{
    public Guid   Id   { get; private set; }
    public string Name { get; private set; } = null!;

    private Category() { }

    internal static Category ForSeed(Guid id, string name)
        => new() { Id = id, Name = name };

    public static Category Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category { Id = Guid.NewGuid(), Name = name };
    }
}
