namespace ProductService.Api.Configuration;

public class KeyVaultConfiguration
{
    public const string SectionName = "KeyVault";

    public string VaultUri { get; set; } = string.Empty;
    public bool UseLocalSecrets { get; set; } = true;
}
