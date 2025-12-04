namespace ProductService.Api.Configuration;

public class MongoDbConfiguration
{
    public const string SectionName = "MongoDB";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "productdb";
    public string CollectionName { get; set; } = "products";
}

