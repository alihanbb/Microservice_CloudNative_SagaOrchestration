using Microsoft.EntityFrameworkCore.Design;

namespace CustomerServices.Infra;

/// <summary>
/// Design-time factory for EF Core migrations
/// This is required because CustomerDbContext has multiple constructors
/// </summary>
public class CustomerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CustomerDbContext>
{
    public CustomerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();
        
        // Default connection string for migrations
        // This will be overridden at runtime by Aspire configuration
        optionsBuilder.UseSqlServer(
            "Server=localhost,1473;Database=CustomerDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", CustomerDbContext.DEFAULT_SCHEMA);
            });

        return new CustomerDbContext(optionsBuilder.Options);
    }
}
