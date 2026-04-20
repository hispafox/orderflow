using Products.API.Tests.Shared.Fixtures;

namespace Products.API.Tests.Shared.Fixtures;

[CollectionDefinition(ProductsApiCollection.Name)]
public class ProductsApiCollection : ICollectionFixture<ProductsApiFactory>
{
    public const string Name = "Products API";
}
