namespace Products.API.Domain;

public class Category
{
    public Guid   Id   { get; private set; }
    public string Name { get; private set; } = null!;

    private Category() { }

    public static Category Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category { Id = Guid.NewGuid(), Name = name };
    }
}
