namespace OrderServices.Api.Configuration;

public class CosmosDbConfiguration
{
    public const string SectionName = "CosmosDb";

    public string DatabaseName { get; set; } = "orderdb";
    public string ContainerName { get; set; } = "orders";
    public string PartitionKey { get; set; } = "/customerId";
    public bool UseEmulator { get; set; } = false;
}
